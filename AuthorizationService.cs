using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Bluegarden.HRPlus.Common;
using Bluegarden.HRPlus.Common.Cache;
using Bluegarden.HRPlus.Common.Constants;
using Bluegarden.HRPlus.Common.Enums;
using Bluegarden.HRPlus.Common.Exceptions;
using Bluegarden.HRPlus.Common.Extensions;
using Bluegarden.HRPlus.DTO.DocumentManagement;
using Bluegarden.HRPlus.Integration;
using Bluegarden.HRPlus.Models;
using Bluegarden.HRPlus.ServiceLayer.DTO;
using Bluegarden.HRPlus.ServiceLayer.DTO.Common;
using Bluegarden.HRPlus.ServiceLayer.DTO.Group;
using Bluegarden.HRPlus.ServiceLayer.Implementation.Helpers;
using Bluegarden.HRPlus.ServiceLayer.SearchFilters;
using RegEx = System.Text.RegularExpressions;
using Bluegarden.HRPlus.ServiceLayer.DTO.Admin;
using Bluegarden.HRPlus.Permission;

namespace Bluegarden.HRPlus.ServiceLayer.Implementation
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IConnectionInfo _connectionInfo;
        private readonly IDependencyInjectionController _dependencyInjectionController;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHRPlusCacheProvider _cache;
        private readonly ICustomerSettingsService _customerSettingsService;
        private readonly IRolePermissionService _rolePermissionService;

        public AuthorizationService(IUnitOfWork unitOfWork, IConnectionInfo connectionInfo, IDependencyInjectionController dependencyInjectionController, IHRPlusCacheProvider cache)
        {
            _unitOfWork = unitOfWork;
            _connectionInfo = connectionInfo;
            _dependencyInjectionController = dependencyInjectionController;
            _cache = cache;

            _customerSettingsService = _dependencyInjectionController.Get<ICustomerSettingsService>();
            _rolePermissionService = _dependencyInjectionController.Get<IRolePermissionService>();
        }

        public List<int> GetGroupUsersAndUserByUserId(int userId)
        {
            var userIds = _dependencyInjectionController.Get<IGroupUserService>().GetGroupUsersByUserId(userId).Select(ugu => ugu.GroupUserId).ToList();
            userIds.Add(userId);

            return userIds;
        }

        public IEnumerable<int> GetUserRoleIds(int userId, IEnumerable<int> roleIds, DateTime checkForDate, bool selectNonGroupApplicableRoles)
        {
            var userRoles = _dependencyInjectionController.Get<IUserRoleService>()
                                                          .GetUserRolesByUserIdAndDate(userId, checkForDate)
                                                          .Where(ur => roleIds.Contains(ur.RoleId));
                                                            
            if (selectNonGroupApplicableRoles)
            {
                userRoles = userRoles.Where(ur => !ur.IsApplicableToGroup);
            }

            return userRoles.Select(ur => ur.Id);
        }

        /// <summary>
        ///     Gets all groups available for the specified <paramref name="caseId" /> and <paramref name="userId" /> that
        ///     are valid for the <paramref name="date" />.
        /// </summary>
        /// <param name="caseId">The id of the case to retrieve available groups for</param>
        /// <param name="userId">The user that requests the groups</param>
        /// <param name="date">The date that the available groups should be between start and end date</param>
        /// <param name="searchTerm">String to search after</param>
        /// <returns>A list of groups that are available based on the input parameters</returns>
        public IEnumerable<GroupDescriptionDTO> GetGroupsAvailableForCaseAndUser(int caseId, int userId, DateTime date, string searchTerm = null)
        {
            return GetGroups(userId, date, caseId, page: 1, pageSize: 20, additionalQuery: g => string.IsNullOrEmpty(searchTerm) || g.Description.Contains(searchTerm), includeOnlyEmployableGroups: true);
        }

        /// <summary>
        ///     Gets all groups available for the specified <paramref name="userId" /> and <paramref name="caseType" /> that
        ///     are valid for the <paramref name="date" />.
        /// </summary>
        /// <param name="userId">The user that requests the groups</param>
        /// <param name="date">The date that the available groups should be between start and end date</param>
        /// <param name="caseType">Type of case</param>
        /// <param name="employerId">Retrieve rows for specified employer</param>
        /// <param name="justWithEmployer">If the query should only retrieve rows that has employer</param>
        /// <param name="searchTerm"></param>
        /// <param name="hideInactive"></param>
        /// <returns>A list of groups that are available based on the input parameters</returns>
        public IEnumerable<GroupDescriptionDTO> GetGroupsAvailableForUser(int userId, DateTime date, CaseType? caseType, int? employerId = null, bool justWithEmployer = false, string searchTerm = null, bool hideInactive = false)
        {
            return GetGroups(userId, date, caseType: caseType, employerId: employerId, justWithEmployer: justWithEmployer,
                             includeOnlyEmployableGroups: true, page: 1, pageSize: 20,
                             additionalQuery: group => (searchTerm == null || group.Description.Contains(searchTerm)) &&
                                                        (!hideInactive || ((group.StartDate == null || group.StartDate <= date) &&
                                                                           (group.EndDate == null || group.EndDate >= date))));
        }

        public List<UserDto> GetMainManagersForGroup(int groupId)
        {
            var mainManagerRoles = _unitOfWork
                                   .RolePermissionRepository
                                   .AsQueryable()
                                   .Where(rp => rp.Permission.Name.Equals(Permissions.IsMainManager))
                                   .Select(rp => rp.Role);

            var mainManagersForGroup = _unitOfWork.UserRoleGroupRepository
                                                 .AsQueryable()
                                                 .Where(x => x.GroupId == groupId
                                                             && mainManagerRoles.Any(mmr => mmr.Id == x.UserRole.RoleId)
                                                             && ((!x.UserRole.StartDate.HasValue || x.UserRole.StartDate.Value.CompareTo(DateTime.Now) <= 0)
                                                                 && (!x.UserRole.EndDate.HasValue || x.UserRole.EndDate.Value.CompareTo(DateTime.Now) >= 0)))
                                                 .Select(x => x.UserRole.User);

            return Mapper.Map<List<UserDto>>(mainManagersForGroup);
        }

        public IQueryable<EmploymentGroup> UserAccessToGroupOnDocumentCategory(int userId, int? documentCategory, DocumentCategoryPermission documentCategoryPermission)
        {
            var userIds = GetGroupUsersAndUserByUserId(userId);

            var roleIds = _dependencyInjectionController.Get<IDocumentCategoryRoleService>().GetDocumentCategoryRoles()
                                                        .Where(dcr => (!documentCategory.HasValue || dcr.CategoryId == documentCategory.Value) && dcr.Permission.HasFlag(documentCategoryPermission))
                                                        .Select(dcr => dcr.RoleId);

            return _unitOfWork.EmploymentGroupRepository.AsQueryable()
                             .Where(eg =>
                                        (eg.StartDate <= DateTime.Today || eg.Employment.StartDate > DateTime.Today) &&
                                        (eg.EndDate == null || eg.EndDate >= DateTime.Today) &&
                                        eg.Group.UserRoleGroups.Any(urg =>
                                                                        userIds.Any(gu => gu == urg.UserRole.UserId) &&
                                                                        urg.UserRole.Role.IsApplicableToGroup &&
                                                                        (urg.UserRole.StartDate == null || urg.UserRole.StartDate <= DateTime.Today) &&
                                                                        (urg.UserRole.EndDate == null || urg.UserRole.EndDate >= DateTime.Today) &&
                                                                        roleIds.Contains(urg.UserRole.RoleId))
                                   );
        }

        public IEnumerable<DocumentCategoryRoleGroupDTO> UserAccessToDocument(int userId)
        {
            var userRoleGroups = _dependencyInjectionController.Get<IUserRoleGroupService>()
                                                               .GetUserRoleGroupsByUserIdAndDate(userId, DateTime.Today)
                                                               .ToArray();

            var documentCategoryRoles = _dependencyInjectionController.Get<IDocumentCategoryRoleService>()
                                                                      .GetDocumentCategoryRoles()
                                                                      .Where(dcr => dcr.Permission.HasFlag(DocumentCategoryPermission.View));

            return documentCategoryRoles.Join(userRoleGroups, dcr => dcr.RoleId, urg => urg.RoleId, (dcr, urg) => new DocumentCategoryRoleGroupDTO
                                                                                                                                       {
                                                                                                                                           RoleId = urg.RoleId,
                                                                                                                                           GroupId = urg.GroupId,
                                                                                                                                           CategoryId = dcr.CategoryId,
                                                                                                                                           Permission = dcr.Permission
                                                                                                                                       });
        }

        public IQueryable<EmploymentGroup> UserAccessToGroups(int userId, int? groupId = null, CaseType? caseType = null)
        {
            var userIds = GetGroupUsersAndUserByUserId(userId);

            return _unitOfWork.EmploymentGroupRepository.AsQueryable()
                              .Where(eg =>
                                         (eg.StartDate <= DateTime.Today || eg.Employment.StartDate > DateTime.Today) &&
                                         (eg.EndDate == null || eg.EndDate >= DateTime.Today) &&
                                         (groupId == null || eg.GroupId == groupId) &&
                                         eg.Group.UserRoleGroups
                                           .Any(urg =>
                                                    userIds.Any(gu => gu == urg.UserRole.UserId) &&
                                                    (urg.UserRole.StartDate == null || urg.UserRole.StartDate <= DateTime.Today) &&
                                                    (urg.UserRole.EndDate == null || urg.UserRole.EndDate >= DateTime.Today) &&
                                                    (
                                                        urg.UserRole.Role.RolePermissions.Any(rp => eg.Employment.EmolumentRecipient && rp.Permission.Name == Permissions.CanViewEmolument) ||
                                                        urg.UserRole.Role.RolePermissions.Any(rp => !eg.Employment.EmolumentRecipient && rp.Permission.Name == Permissions.CanViewEmployment)
                                                    ) &&
                                                    (caseType == null || urg.UserRole.Role.CaseDefinitionRoleActions.Any(cdra => cdra.CaseDefinitionAction.CaseDefinition.CaseType == caseType))));
        }

        /// <summary>
        ///     Gets all available groups
        /// </summary>
        /// <returns>A list of groups as KeyValueDTO</returns>
        public IEnumerable<KeyValueDTO<string>> GetAllGroups()
        {
            var query = _unitOfWork.GroupItemRepository.AsQueryable().Select(row => new KeyValueDTO<string> { Key = row.Id, Value = row.Description });
            return CacheHelper.GetCacheData(typeof(Group), query, _cache, _connectionInfo, query);
        }

        private int? GetUserIdByUserName(string userName)
        {
            if (_connectionInfo.UserName == userName)
            {
                return _connectionInfo.UserId;
            }

            return _unitOfWork.UserRepository.AsQueryable().Where(u => u.Username == userName).Select(u => (int?)u.Id).FirstOrDefault();
        }

        /// <summary>
        ///     Gets available groups for employer on specified date
        /// </summary>
        /// <param name="employerId">Retrieve rows for specified employer</param>
        /// <param name="searchTerm">String to search after</param>
        /// <param name="date">The date that the available groups should be between start and end date</param>
        /// <param name="page">The page to display</param>
        /// <param name="pageSize">The number of records per page</param>
        /// <returns>A list of GroupDTO</returns>
        public IEnumerable<GroupDTO> GetGroupsByEmployerId(int? employerId, string searchTerm, DateTime? date = null, int? page = null, int? pageSize = null)
        {
            if (date == null)
            {
                date = DateTime.Today;
            }

            var query = _unitOfWork.GroupRepository.AsQueryable().Where(d =>
                                                                            d.EmployerId == employerId &&
                                                                            (d.StartDate <= date || d.StartDate == null) &&
                                                                            (d.EndDate >= date || d.EndDate == null));

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.ApplyBasicQueryFilter(searchTerm, new[] { nameof(Group.Id), nameof(Group.Description) });
            }

            query = query.OrderBy(d => d.Description);

            if (page != null && pageSize != null)
            {
                query = query.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
            }

            return query.ProjectTo<GroupDTO>().ToArray();
        }

        public IList<int> GetAllowedGroups(int userRoleId)
        {
            return GetAllowedGroups(new[] { userRoleId });
        }

        public void AttachGroupsToUserRole(int userRoleId, List<int> checkedGroupList)
        {
            using (var scope = TransactionHelpers.CreateReadCommittedScope())
            {
                using (var context = new HRPlusEntitiesContext(_connectionInfo))
                {
                    using (var table = new DataTable())
                    {
                        table.Columns.Add("GroupId", typeof(int));

                        foreach (var groupId in checkedGroupList)
                        {
                            if (groupId > 0)
                            {
                                table.Rows.Add(groupId);
                            }
                        }
                        context.Database.ExecuteSqlCommand("EXEC [UpdateUserRoleGroups] @CurrentUser, @UserRoleId, @GroupIds",
                                                           new SqlParameter("@CurrentUser", _connectionInfo.UserName),
                                                           new SqlParameter("@UserRoleId", userRoleId),
                                                           new SqlParameter("@GroupIds", SqlDbType.Structured)
                                                           {
                                                               TypeName = "[dbo].[UpdateUserRoleGroupsTable]",
                                                               Value = table
                                                           });
                    }
                }

                scope?.Complete();
            }
        }

        /// <inheritdoc />
        public void DeleteUserRoleGroupsByRoleId(int id)
        {
            using (var context = new HRPlusEntitiesContext(_connectionInfo))
            {
                context.Database.ExecuteSqlCommand("EXEC [DeleteUserRoleGroupsByRoleId] @RoleId", new SqlParameter("@RoleId", id));
            }
        }

        public void RefreshQwPermissions(params int[] userIds)
        {
            if (_connectionInfo.ConnectionString == null)
            {
                // Exit for unit tests
                return;
            }

            using (var context = new HRPlusEntitiesContext(_connectionInfo))
            {
                foreach (var userId in userIds)
                {
                    context.Database.ExecuteSqlCommand("EXEC [RefreshQWPermissions] @UserId", new SqlParameter("@UserId", userId));
                }
            }
        }

        /// <summary>
        ///     Method which answer the question "Do you user have this permission."
        /// </summary>
        /// <returns>True if user have the permission. False in other case.</returns>
        public bool HasPermission(string moduleName, string permissionName, int userId)
        {
            return _rolePermissionService.HasPermission(userId, moduleName, permissionName);
        }

        /// <summary>
        ///     Checks if the specified <paramref name="userName" /> has access to the specified <paramref name="moduleName" /> and
        ///     <paramref name="permissionName" />
        /// </summary>
        /// <param name="userName">The name of the user to check</param>
        /// <param name="moduleName">The name of the module that the permission belongs to</param>
        /// <param name="permissionName">The name of the permission to check</param>
        /// <returns>True if the user has access, otherwise false</returns>
        public bool HasPermission(string userName, string moduleName, string permissionName)
        {
            var userId = GetUserIdByUserName(userName);

            return userId.HasValue && _rolePermissionService.HasPermission(userId.Value, moduleName, permissionName);
        }

        /// <summary>
        ///     Gets all available permission for the specified <paramref name="userName" />
        /// </summary>
        /// <param name="userName">The name of the user to retrieve permissions for</param>
        /// <returns>A list of available permissions for the specified user</returns>
        public string[] GetModulePermissionsByUserName(string userName)
        {
            var userId = GetUserIdByUserName(userName);

            if (userId == null)
            {
                return new string[0];
            }

            return _dependencyInjectionController.Get<IRolePermissionService>()
                                                 .GetRolePermissionsByUserId(userId.Value)
                                                 .Select(p => p.Module + "." + p.Permission)
                                                 .ToArray();
        }

        /// <summary>
        ///     Gets all available permission in the specified <param name="moduleName" />for the specified <paramref name="userName" />
        ///     GroupUsers are not included in result!
        /// </summary>
        /// <param name="userName">The name of the user to retrieve permissions for</param>
        /// <param name="moduleName">The name of the module to retrieve permissions for</param>
        /// <returns>A list of available permissions for the specified user</returns>
        public string[] GetModulePermissionsByUserName(string userName, string moduleName)
        {
            var userId = GetUserIdByUserName(userName);

            if (userId == null)
            {
                return new string[0];
            }

            var rolePermissions = _rolePermissionService.GetRolePermissionsByUserId(userId.Value);

            if (moduleName != null)
            {
                rolePermissions = rolePermissions.Where(rp => rp.Module == moduleName);
            }

            return rolePermissions.Select(rp => rp.Module + "." + rp.Permission).ToArray();
        }


        /// <summary>
        ///     Gets the name of the employer connected to the group with <paramref name="groupId" />
        /// </summary>
        /// <param name="groupId">The id of the group to retrieve employer from</param>
        /// <returns>The name of the employer that is connected to <paramref name="groupId" /></returns>
        public string GetEmployerName(int groupId)
        {
            if (groupId <= 0)
            {
                throw new ArgumentException(ErrorMessages.ShouldBeGreaterThan0, nameof(groupId));
            }

            return _unitOfWork.GroupRepository.AsQueryable()
                              .Where(group =>
                                         group.Id == groupId &&
                                         group.Employer.CompanyCode != General.CompanyCodeZero &&
                                         group.Employer.UnitCode != General.UnitCodeZero
                                    )
                              .Select(group => group.Employer.Name)
                              .FirstOrDefault();
        }

        /// <summary>
        ///     Gets the name of the group specified by <paramref name="groupId" />
        /// </summary>
        /// <param name="groupId">The id of the group</param>
        /// <returns>The name if the group exists, otherwise null</returns>
        public string GetGroupName(int groupId)
        {
            if (groupId < 0)
            {
                throw new ArgumentException(ErrorMessages.ShouldBeGreaterOrEqualTo0, nameof(groupId));
            }

            return GetAllGroups().Where(g => g.Key == groupId).Select(g => g.Value).FirstOrDefault();
        }

        public IList<DelegationUserDto> GetDelegationUsers(int userRoleId, string searchTerm)
        {
            const int pageSize = 20;

            var selectProtectedUsers = HasPermission(Modules.EmploymentManagement, Permissions.CanAdministrateProtectedIdentities, _connectionInfo.UserId);

            if (_customerSettingsService.GetAuthorizationSettings().UseGroupBasedDelegation)
            {
                var userList = new List<DelegationUserDto>();

                var groupIds = _unitOfWork.UserRoleGroupRepository.AsQueryable()
                                          .Where(urg => urg.UserRole.Id == userRoleId &&
                                                        (urg.UserRole.StartDate == null || urg.UserRole.StartDate <= DateTime.Today) &&
                                                        (urg.UserRole.EndDate == null || urg.UserRole.EndDate >= DateTime.Today) &&
                                                        (urg.Group.StartDate == null || urg.Group.StartDate <= DateTime.Today) &&
                                                        (urg.Group.EndDate == null || urg.Group.EndDate >= DateTime.Today) &&
                                                        urg.UserRole.Role.IsDelegering && urg.UserRole.Role.IsApplicableToGroup &&
                                                        urg.UserRole.UserId == _connectionInfo.UserId)
                                          .Select(urg => urg.GroupId)
                                          .Distinct()
                                          .ToArray();

                userList.AddRange(_unitOfWork.UserRoleGroupRepository.AsQueryable()
                                             .Where(urg => urg.UserRole.UserId != _connectionInfo.UserId &&
                                                           urg.UserRole.User.IsEnabled && !urg.UserRole.User.GroupUser &&
                                                           (urg.UserRole.StartDate == null || urg.UserRole.StartDate <= DateTime.Today) &&
                                                           (urg.UserRole.EndDate == null || urg.UserRole.EndDate >= DateTime.Today) &&
                                                           (searchTerm == null || urg.UserRole.User.Username.Contains(searchTerm) || urg.UserRole.User.FullName.Contains(searchTerm)) &&
                                                           groupIds.Contains(urg.GroupId))
                                             .Select(urg => new DelegationUserDto
                                             {
                                                 Id = urg.UserRole.UserId,
                                                 Username = urg.UserRole.User.Username,
                                                 FullName = urg.UserRole.User.FullName
                                             })
                                             .Distinct()
                                             .OrderBy(u => u.Username)
                                             .Take(pageSize)
                                             .ToArray());

                userList.AddRange(_unitOfWork.EmploymentGroupRepository.AsQueryable()
                                             .Where(eg =>
                                                        (eg.StartDate <= DateTime.Today || eg.Employment.StartDate > DateTime.Today) &&
                                                        (eg.EndDate == null || eg.EndDate >= DateTime.Today) &&
                                                        groupIds.Contains(eg.GroupId) &&
                                                        eg.Employment.Employee.Person.UserId != _connectionInfo.UserId &&
                                                        eg.Employment.Employee.Person.User.IsEnabled && !eg.Employment.Employee.Person.User.GroupUser &&
                                                        (searchTerm == null || eg.Employment.Employee.Person.User.Username.Contains(searchTerm) || eg.Employment.Employee.Person.User.FullName.Contains(searchTerm)) &&
                                                        (selectProtectedUsers || eg.Employment.Employee.Person.ProtectedIdentity != ProtectedIdentityType.ProtectedIdentity)
                                                   )
                                             .Select(eg => new DelegationUserDto
                                             {
                                                 Id = eg.Employment.Employee.Person.UserId,
                                                 Username = eg.Employment.Employee.Person.User.Username,
                                                 FullName = eg.Employment.Employee.Person.User.FullName
                                             })
                                             .Distinct()
                                             .OrderBy(u => u.Username)
                                             .Take(pageSize)
                                             .ToArray());

                for (var i = 0; i < userList.Count; i++)
                {
                    if (userList.Any(u => u.Id == userList[i].Id && u != userList[i]))
                    {
                        userList.RemoveAt(i);
                    }
                }

                return userList.OrderBy(u => u.Username).Take(pageSize).ToList();
            }

            var rolesOfManagerType = _unitOfWork.RoleRepository.AsQueryable().Where(x => x.Type.Equals(RoleType.Manager.ToString())).Select(x => x.Id);

            var result = _unitOfWork.UserRoleRepository.AsQueryable()
                                    .Where(ur => rolesOfManagerType.Contains(ur.RoleId) &&
                                                 ur.User.IsEnabled &&
                                                 !ur.User.GroupUser &&
                                                 (searchTerm == null || ur.User.Username.Contains(searchTerm) || ur.User.FullName.Contains(searchTerm)) &&
                                                 (selectProtectedUsers || !ur.User.Persons.Any() || ur.User.Persons.Any(p => p.ProtectedIdentity != ProtectedIdentityType.ProtectedIdentity)))
                                    .Select(ur => new DelegationUserDto
                                    {
                                        Id = ur.UserId,
                                        Username = ur.User.Username,
                                        FullName = ur.User.FullName
                                    })
                                    .Distinct();

            return result.OrderBy(u => u.Username).Take(pageSize).ToList();
        }

        public IQueryable<UserRolesAdminDTO> GetDelegatedUserById(int id) 
        {
            var showProtectedUsers = _rolePermissionService.HasPermission(_connectionInfo.UserId, Modules.EmploymentManagement, Permissions.CanAdministrateProtectedIdentities);

            var query = _unitOfWork.UserRepository.Where(user =>
                (showProtectedUsers || !user.Persons.Any() || user.Persons.Any(p => p.ProtectedIdentity != ProtectedIdentityType.ProtectedIdentity)) &&
                (user.Id == id));

            return query.ProjectTo<UserRolesAdminDTO>();
        }

        public IList<UserRolesAdminDTO> GetDelegableUserByRoleId(int roleId)
        {
            var receiverDelegatedRoleIds = _unitOfWork.ReceiverDelegationRolesRepository.AsQueryable().Where(rd => rd.ReceiverDelegationRoleId == roleId)
                                                                              .Select(rd => rd.ReceiverRoleId)
                                                                              .ToArray();
            List<int[]> receiverUserIds = new List<int[]>();

            foreach (var receiverDelegationRoleId in receiverDelegatedRoleIds)
            {
                var receiverUserId = _unitOfWork.UserRoleRepository.AsQueryable().Where(ur => ur.RoleId == receiverDelegationRoleId)
                                                                              .Select(ur => ur.UserId)
                                                                              .ToArray();
                receiverUserIds.Add(receiverUserId);
            }

            var delegableRoleReceivers = new List<UserRolesAdminDTO>();

            for (var i = 0; i < receiverUserIds.Count; i++)
            {
                foreach (var receiverUserId in receiverUserIds[i])
                {
                    var delegableRoleReceiver = GetDelegableUsersById(receiverUserId);
                    delegableRoleReceivers.AddRange(delegableRoleReceiver);
                }
            }
            return delegableRoleReceivers;
        }

        private IQueryable<UserRolesAdminDTO> GetDelegableUsersById(int userId)
        {
            var showProtectedUsers = _rolePermissionService.HasPermission(_connectionInfo.UserId, Modules.EmploymentManagement, Permissions.CanAdministrateProtectedIdentities);

            return _unitOfWork.UserRepository.Where(user =>
                    (showProtectedUsers || !user.Persons.Any() || user.Persons.Any(p => p.ProtectedIdentity != ProtectedIdentityType.ProtectedIdentity)) &&
                    (user.Id == userId)).ProjectTo<UserRolesAdminDTO>();
        }

        public IEnumerable<UserAdminDTO> GetUsersForAdminView(int? pageNumber, int? pageSize, string sortBy, string sortDir, out int totalUsersCount)
        {
            var showProtectedUsers = HasPermission(Modules.EmploymentManagement, Permissions.CanAdministrateProtectedIdentities, _connectionInfo.UserId);

            var users = _unitOfWork.UserRepository.AsQueryable().Where(user => showProtectedUsers ||
                                                                               user.Persons.Any(p => p.ProtectedIdentity != ProtectedIdentityType.ProtectedIdentity));

            if (!pageNumber.HasValue)
            {
                var resultList = users.ProjectTo<UserAdminDTO>().ToArray();
                totalUsersCount = resultList.Length;

                return resultList;
            }

            IQueryable<User> result = (sortDir == "ASC") ? users.OrderBy(sortBy) : users.OrderByDescending(sortBy);

            if (pageSize.HasValue)
            {
                result = result.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value);
            }

            totalUsersCount = _unitOfWork.UserRepository.AsQueryable().Count();

            return result.ProjectTo<UserAdminDTO>().ToArray();
        }

        /// <summary>
        ///     Gets a user
        /// </summary>
        /// <param name="id">The id of the user to get</param>
        /// <returns>A user as a <see cref="UserDto" /></returns>
        public UserDto GetUser(int id)
        {
            return _unitOfWork.UserRepository.AsQueryable().ProjectTo<UserDto>().FirstOrDefault(x => x.Id == id);
        }

        /// <summary>
        ///     Gets a user for admin views
        /// </summary>
        /// <param name="id">The id of the user to get</param>
        /// <returns>A user as a <see cref="UserAdminDTO" /></returns>
        public UserAdminDTO GetUserForAdminView(int id)
        {
            return _unitOfWork.UserRepository.AsQueryable().ProjectTo<UserAdminDTO>().FirstOrDefault(x => x.Id == id);
        }

        public IList<UserDto> GetUsersByPermission(string permissionName)
        {
            var permissionId = _unitOfWork
                .PermissionRepository
                .AsQueryable()
                .Where(permission => permission.Name == permissionName)
                .Select(permission => permission.Id)
                .FirstOrDefault();

            if (permissionId > 0)
            {
                return _unitOfWork.UserRepository.AsQueryable()
                                  .Where(user => user.UserRoles.Any(
                                                                    userRole => userRole.Role.RolePermissions.Any(
                                                                                                                  role => role.PermissionId == permissionId)))
                                  .ProjectTo<UserDto>()
                                  .ToList();
            }
            return new List<UserDto>();
        }

        /// <summary>
        ///     Gets a IQueryable of <see cref="UserAdminDTO" />s, filtered by <paramref name="filter" /> for an admin view
        /// </summary>
        /// <param name="filter">Used to filter which users should be returned</param>
        /// <param name="totalUsersCount">The total number of existing users</param>
        /// <returns>An IQueryable of <see cref="UserAdminDTO" /></returns>
        public IQueryable<UserAdminDTO> GetUsersSearchQueryForAdminViews(UserFilter filter, out int totalUsersCount)
        {
            var showProtectedUsers = _rolePermissionService.HasPermission(_connectionInfo.UserId, Modules.EmploymentManagement,
                                                                          Permissions.CanAdministrateProtectedIdentities);

            var sc = filter?.SearchCriteria?.Trim();

            var query = _unitOfWork.UserRepository.Where(user =>
                (showProtectedUsers || !user.Persons.Any() || user.Persons.Any(p => p.ProtectedIdentity != ProtectedIdentityType.ProtectedIdentity)) &&
                (filter.RoleId == null || user.Id == filter.UserId || user.UserRoles.Any(ur => ur.RoleId == filter.RoleId)));

            if (!string.IsNullOrEmpty(sc))
            {
                switch (filter.Column)
                {
                    case FilterColumn.Username:
                        query = query.Where(user => user.Username.Contains(sc));
                        break;
                    case FilterColumn.FirstName:
                        query = query.Where(user => user.FirstName.Contains(sc));
                        break;
                    case FilterColumn.LastName:
                        query = query.Where(user => user.LastName.Contains(sc));
                        break;
                    case FilterColumn.Email:
                        query = query.Where(user => user.Email.Contains(sc));
                        break;
                    case FilterColumn.FullName:
                        query = query.Where(user => user.FullName.Contains(sc));
                        break;
                    case FilterColumn.WorkNumber:
                        query = query.Where(user => user.Persons.Any(person =>
                            person.Employees.Any(emp =>
                                emp.WorkNumber.Contains(sc))));
                        break;
                    case FilterColumn.EmploymentNumber:
                        query = query.Where(user => user.Persons.Any(person =>
                            person.Employees.Any(emp =>
                                emp.Employments.Any(empl =>
                                    empl.EmploymentNumber.Contains(sc) &&
                                    (empl.StartDate == null || empl.StartDate <= DateTime.Today) &&
                                    (empl.EndDate == null || empl.EndDate >= DateTime.Today)))));
                        break;
                    case FilterColumn.Position:
                        query = query.Where(user => user.Persons.Any(person =>
                            person.Employees.Any(emp =>
                                emp.Employments.Any(empl =>
                                    empl.EmploymentPositions.FirstOrDefault(p =>
                                        p.StartDate <= DateTime.Today &&
                                        (p.EndDate == null || p.EndDate >= DateTime.Today))
                                        .Position.Description.Contains(sc) &&
                                    (empl.StartDate == null || empl.StartDate <= DateTime.Today) &&
                                    (empl.EndDate == null || empl.EndDate >= DateTime.Today)))));
                        break;
                    case FilterColumn.AccountPart1:
                        query = query.Where(user => user.Persons.Any(person =>
                            person.Employees.Any(emp =>
                                emp.Employments.Any(empl =>
                                    empl.SalaryAffectings.Any(se => se.Department.AccountPart1.Contains(sc)) &&
                                    (empl.StartDate == null || empl.StartDate <= DateTime.Today) &&
                                    (empl.EndDate == null || empl.EndDate >= DateTime.Today)))));
                        break;
                }
            }

            totalUsersCount = _unitOfWork.UserRepository.AsQueryable().Count();

            return query.OrderBy(u => u.Username).Take(100).ProjectTo<UserAdminDTO>();
        }

        /// <summary>
        ///     Gets a IQueryable of <see cref="UserRolesAdminDTO" />s, filtered by <paramref name="filter" /> for an admin view
        /// </summary>
        /// <param name="filter">Used to filter which users should be returned</param>
        /// <returns>An IQueryable of <see cref="UserRolesAdminDTO" /></returns>
        public IQueryable<UserRolesAdminDTO> GetUsersSearchQueryForAdminViewsWithRoles(UserFilter filter)
        {

            var showProtectedUsers = _rolePermissionService.HasPermission(_connectionInfo.UserId, Modules.EmploymentManagement,
                                                              Permissions.CanAdministrateProtectedIdentities);

            var sc = filter?.SearchCriteria?.Trim();

            var query = _unitOfWork.UserRepository.Where(user =>
                (showProtectedUsers || !user.Persons.Any() || user.Persons.Any(p => p.ProtectedIdentity != ProtectedIdentityType.ProtectedIdentity)) &&
                (filter.RoleId == null || user.Id == filter.UserId || user.UserRoles.Any(ur => ur.RoleId == filter.RoleId)));

            if (!string.IsNullOrEmpty(sc))
            {
                switch (filter.Column)
                {
                    case FilterColumn.Username:
                        query = query.Where(user => user.Username.Contains(sc));
                        break;
                    case FilterColumn.FirstName:
                        query = query.Where(user => user.FirstName.Contains(sc));
                        break;
                    case FilterColumn.LastName:
                        query = query.Where(user => user.LastName.Contains(sc));
                        break;
                    case FilterColumn.Email:
                        query = query.Where(user => user.Email.Contains(sc));
                        break;
                    case FilterColumn.FullName:
                        query = query.Where(user => user.FullName.Contains(sc));
                        break;
                    case FilterColumn.WorkNumber:
                        query = query.Where(user => user.Persons.Any(person =>
                            person.Employees.Any(emp =>
                                emp.WorkNumber.Contains(sc))));
                        break;
                    case FilterColumn.EmploymentNumber:
                        query = query.Where(user => user.Persons.Any(person =>
                            person.Employees.Any(emp =>
                                emp.Employments.Any(empl =>
                                    empl.EmploymentNumber.Contains(sc) &&
                                    (empl.StartDate == null || empl.StartDate <= DateTime.Today) &&
                                    (empl.EndDate == null || empl.EndDate >= DateTime.Today)))));
                        break;
                    case FilterColumn.Position:
                        query = query.Where(user => user.Persons.Any(person =>
                            person.Employees.Any(emp =>
                                emp.Employments.Any(empl =>
                                    empl.EmploymentPositions.FirstOrDefault(p =>
                                        p.StartDate <= DateTime.Today &&
                                        (p.EndDate == null || p.EndDate >= DateTime.Today))
                                        .Position.Description.Contains(sc) &&
                                    (empl.StartDate == null || empl.StartDate <= DateTime.Today) &&
                                    (empl.EndDate == null || empl.EndDate >= DateTime.Today)))));
                        break;
                    case FilterColumn.AccountPart1:
                        query = query.Where(user => user.Persons.Any(person =>
                            person.Employees.Any(emp =>
                                emp.Employments.Any(empl =>
                                    empl.SalaryAffectings.Any(se => se.Department.AccountPart1.Contains(sc)) &&
                                    (empl.StartDate == null || empl.StartDate <= DateTime.Today) &&
                                    (empl.EndDate == null || empl.EndDate >= DateTime.Today)))));
                        break;
                }
            }


            return query.OrderBy(u => u.Username).Take(100).ProjectTo<UserRolesAdminDTO>();
        }

        /// <summary>
        /// Adds a new user to the database and returns its generated id.
        /// </summary>
        /// <param name="userDto"></param>
        /// <returns></returns>
        public int AddUser(UserDto userDto)
        {
            if (UserNameIsInUse(userDto.Id, userDto.Username))
            {
                throw new UserLoginExistsExeption(string.Concat("Username exists: ", userDto.Username));
            }

            if (!NumericPhoneNumber(userDto.PhoneNumber))
            {
                throw new NonNumericPhoneNumberException(string.Concat("Non numeric phone number: ", userDto.PhoneNumber));
            }

            var user = Mapper.Map<User>(userDto);

            user.IsEnabled = true;
            user.ManuallyCreated = true;
            _unitOfWork.UserRepository.Add(user);

            var userGroupUsers = Mapper.Map<UserGroupUser[]>(userDto.UserGroupUsers);
            UpdateUserGroupUsers(userGroupUsers);

            _unitOfWork.Save();

            RefreshQwPermissions(user.Id);

            return user.Id;
        }

        public void UpdateUser(UserDto userDto, string command = null)
        {
            if (UserNameIsInUse(userDto.Id, userDto.Username))
            {
                throw new UserLoginExistsExeption(string.Concat("User login name exists : ", userDto.Username));
            }

            if (!NumericPhoneNumber(userDto.PhoneNumber))
            {
                throw new NonNumericPhoneNumberException(string.Concat("Non numeric phone number: ", userDto.PhoneNumber));
            }

            var existingModel = _unitOfWork.UserRepository.AsQueryable().FirstOrDefault(x => x.Id == userDto.Id);

            var tempUserImage = userDto.Image;

            var sendBackendEvent = userDto.Username != existingModel?.Username || userDto.Email != existingModel?.Email;

            var user = Mapper.Map(userDto, existingModel);

            if (command == CommandNames.Delete)
            {
                user.Image = null;
            }

            if (tempUserImage != null && tempUserImage.Length > 0)
            {
                user.Image = ScaleUserImage(userDto.Image);
            }

            if (user.IsEnabled && user.DeactivationReason != null)
            {
                user.DeactivationReason = null;
            }

            _unitOfWork.UserRepository.Update(user);

            var userGroupUsers = Mapper.Map<UserGroupUser[]>(userDto.UserGroupUsers);

            UpdateUserGroupUsers(userGroupUsers, existingModel?.UserGroupUsers.ToArray());

            var groupUserUsers = Mapper.Map<IEnumerable<UserGroupUser>>(userDto.GroupUserUsers).ToArray();
            UpdateUserGroupUsers(groupUserUsers, existingModel?.GroupUserUsers.ToArray());

            if (sendBackendEvent)
            {
                AddBackendEvent(user.Id);
            }

            _unitOfWork.Save();

            RefreshQwPermissions(user.Id);
        }

        private void AddBackendEvent(int userId)
        {
            var personId = _unitOfWork.PersonRepository.AsQueryable().FirstOrDefault(p => p.UserId == userId)?.Id;

            if (!personId.HasValue)
            {
                return;
            }

            var isMedvind = _unitOfWork.EmploymentRepository.AsQueryable().Any(e => e.Employee.PersonId == personId.Value && e.TimeSystem.TimeSystemCode == "2");

            _unitOfWork.EventRepository.Add(new Models.Backend.Event
            {
                Status = (byte)(isMedvind ? 6 : 3),
                Type = (int)BackendType.std_pers_Persons,
                Action = 1,
                Created = DateTime.Now,
                Message = $"{personId.Value}",
                Log = string.Empty,
                PublishedToExternalSystem = 1,
                FECurrentId = personId,
                FEPersonId = personId
            });
        }

        /// <summary>
        ///     Method which answer the question of user can work with given employment by given permission.
        /// </summary>
        /// <returns>True if user can work with current employment. False in other case.</returns>
        public bool HasUserEmploymentPermissions(string moduleName, int userId, string permissionName, int employmentId)
        {
            var userHasPermission = false;

            var isUsersEmployment = _unitOfWork.EmployeeRepository.AsQueryable().Include(x => x.Employments).Any(x => x.Person.UserId == userId && x.Employments.Any(empl => empl.Id == employmentId));

            var permissionRoleIds = _rolePermissionService.GetRolesByModuleAndPermission(moduleName, permissionName).ToArray();

            if (isUsersEmployment)
            {
                var userNotApplicableToGroupsRoles = GetUserRoleIds(userId, permissionRoleIds, DateTime.Today, true);
                if (userNotApplicableToGroupsRoles.Any())
                {
                    userHasPermission = true;
                }
            }

            if (!userHasPermission)
            {
                var userRoleIds = GetUserRoleIds(userId, permissionRoleIds, DateTime.Today, false).ToArray();
                var groupIds = GetAllowedGroups(userRoleIds);
                userHasPermission = EmploymentBelongsToOneOrMoreGroups(employmentId, groupIds);
            }

            return userHasPermission;
        }

        public bool HasUserCreateNewReportForEmploymentPermission(int userId, int employmentId)
        {
            return EmployeeService.GetAllowedEmploymentsAsQueryable(_unitOfWork, userId, Permissions.CanEnterTravelExpenseReportPermission,
                                                                    Modules.ExpenseTravel, false)
                                  .Any(x => x.Id == employmentId);
        }

        public List<UserDto> GetMainManagersForGroup(int? groupId)
        {
            var mainManagerRoles = _unitOfWork.RolePermissionRepository.AsQueryable()
                                              .Include(x => x.Permission)
                                              .Include(x => x.Role)
                                              .Where(x => x.Permission.Name.Equals(Permissions.IsMainManager))
                                              .Select(x => x.Role);

            var mainManagers = Mapper.Map<List<UserDto>>(_unitOfWork.UserRoleGroupRepository
                                                                    .AsQueryable()
                                                                    .Include(x => x.UserRole)
                                                                    .Include(x => x.UserRole.User)
                                                                    .Where(x => x.GroupId == groupId.Value &&
                                                                                (x.UserRole.StartDate == null || x.UserRole.StartDate <= DateTime.Today) &&
                                                                                (x.UserRole.EndDate == null || x.UserRole.EndDate >= DateTime.Today) &&
                                                                                mainManagerRoles.Any(mmr => mmr.Id == x.UserRole.RoleId))
                                                                    .Select(x => x.UserRole.User))
                                     .Distinct();

            return mainManagers.ToList();
        }

        /// <summary>
        ///     Returns true if person of employment is the current logged in user
        /// </summary>
        /// <param name="personId">person id </param>
        /// <param name="userId">logged in user id </param>
        /// <returns>bool</returns>
        public bool IsEmployeeLoggedInUser(int? personId, int userId)
        {
            return _unitOfWork.PersonRepository.AsQueryable().Any(x => x.Id == personId && x.UserId == userId);
        }

        /// <summary>
        ///     Takes a list of existing user group users and a new set of user group
        ///     users and updates the database.
        /// </summary>
        /// <param name="userGroupUsers">New set of user group users.</param>
        /// <param name="existingUserGroupUsers">List of existing user group users.</param>
        private void UpdateUserGroupUsers(UserGroupUser[] userGroupUsers, UserGroupUser[] existingUserGroupUsers = null)
        {
            existingUserGroupUsers = existingUserGroupUsers ?? new UserGroupUser[0];

            var userGroupUserComparer = new UserGroupUserComparer();

            var newUserGroupUsers = userGroupUsers.Except(existingUserGroupUsers, userGroupUserComparer);

            foreach (var newUserGroupUser in newUserGroupUsers)
            {
                _unitOfWork.UserGroupUserRepository.Add(newUserGroupUser);
            }

            var removedUserGroupUsers = existingUserGroupUsers.Except(userGroupUsers, userGroupUserComparer);

            foreach (var removedUserGroupUser in removedUserGroupUsers)
            {
                _unitOfWork.UserGroupUserRepository.Delete(removedUserGroupUser);
            }
        }

        private IEnumerable<GroupDescriptionDTO> GetGroups(int userId, DateTime date, int? caseId = null, CaseType? caseType = null, int? employerId = null,
                                                           bool justWithEmployer = false, Expression<Func<Group, bool>> additionalQuery = null, bool includeOnlyEmployableGroups = false, int? page = null, int? pageSize = null)
        {
            if (userId <= 0)
            {
                throw new ArgumentException(ErrorMessages.ShouldBeGreaterThan0, nameof(userId));
            }

            if (caseId != null && caseType != null)
            {
                throw new ArgumentException(@"caseId and caseType cannot both be set", nameof(caseId));
            }

            if (caseId != null)
            {
                caseType = _dependencyInjectionController.Get<ICaseInfoService>().GetCaseInfoByCaseId(caseId.Value).CaseDefinition.CaseType ?? CaseType.Unknown;
            }

            var userIds = GetGroupUsersAndUserByUserId(userId);

            var validUserRoles = _unitOfWork.UserRoleRepository.AsQueryable()
                                            .Where(ur => userIds.Contains(ur.UserId) &&
                                                         (ur.StartDate == null || ur.StartDate <= date) &&
                                                         (ur.EndDate == null || (ur.EndDate >= date && ur.EndDate >= DateTime.Today)) &&
                                                         (caseType == null ||
                                                          ur.Role.CaseDefinitionRoleActions.Any(cdra => cdra.CaseDefinitionAction.CaseAction.Action == CaseAction.Save &&
                                                                                                        cdra.CaseDefinitionAction.CaseDefinition.CaseType == caseType)));

            var query = _unitOfWork.GroupRepository.AsQueryable()
                                   .Where(g => g.UserRoleGroups.Any(urg => validUserRoles.Contains(urg.UserRole)) &&
                                               g.Employer.CompanyCode != General.CompanyCodeZero &&
                                               g.Employer.UnitCode != General.UnitCodeZero);

            if (caseId != null)
            {
                var caseEmployerId = _unitOfWork.TempEmployeeRepository.AsQueryable()
                                            .Where(te => te.CaseId == caseId)
                                            .Select(te => te.EmployerId)
                                            .FirstOrDefault();

                if (caseEmployerId != null)
                {
                    query = query.Where(g => g.EmployerId == caseEmployerId);
                }
            }
            else if (employerId != null)
            {
                query = query.Where(g => g.EmployerId == employerId);
            }
            else if (justWithEmployer)
            {
                query = query.Where(g => g.EmployerId != null);
            }

            if (caseType != null)
            {
                if (caseType == CaseType.NewEmployment || caseType == CaseType.MoveEmploymentRequest)
                {
                    query = query.Where(g => (g.StartDate == null || g.StartDate <= date));
                }

                query = query.Where(g => (g.EndDate == null || g.EndDate >= date));
            }

            if (additionalQuery != null)
            {
                query = query.Where(additionalQuery);
            }

            if (includeOnlyEmployableGroups)
            {
                query = query.Where(g => g.Type.IsEmployable);
            }

            query = query.OrderBy(group => group.Description);

            if (page != null && pageSize != null)
            {
                query = query.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
            }

            return query.ProjectTo<GroupDescriptionDTO>().ToArray();
        }

        private bool EmploymentBelongsToOneOrMoreGroups(int employmentId, IEnumerable<int> groupIds)
        {
            return _unitOfWork.EmploymentGroupRepository.AsQueryable().Any(eg => eg.EmploymentId == employmentId && groupIds.Contains(eg.GroupId));
        }

        private bool UserNameIsInUse(int userId, string userName)
        {
            return _unitOfWork.UserRepository.AsQueryable()
                              .Any(u => u.Username.Equals(userName, StringComparison.OrdinalIgnoreCase) && u.Id != userId);
        }

        private static bool NumericPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return true;
            }
            const string pattern = @"^(?:\+[0-9]{0,3}(?:\ ?\(0\))?|0)[ 0-9]+\-?[ 0-9]*$";

            var regex = new RegEx.Regex(pattern, RegEx.RegexOptions.IgnoreCase);

            return regex.IsMatch(phoneNumber);

        }

        private IList<int> GetAllowedGroups(int[] userRoleIds)
        {
            return _dependencyInjectionController.Get<IUserRoleGroupService>()
                                                 .GetUserRoleGroupsByUserRoleIds(userRoleIds)
                                                 .Select(row => row.GroupId).ToList();
        }

        private static byte[] ScaleUserImage(byte[] imageData)
        {
            const int width = Files.UserImageWidth;
            Image image;

            using (var ms = new MemoryStream(imageData))
            {
                image = new Bitmap(ms);
            }

            if (image.Width <= width)
            {
                return imageData;
            }

            var ratioX = width / (double)image.Width;
            var height = (int)(image.Height * ratioX);

            var scaledImage = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            scaledImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(scaledImage))
            {
                graphics.Clear(Color.White);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                graphics.DrawImage(image, 0, 0, width, height);
            }

            byte[] resizedImage;
            using (var ms = new MemoryStream())
            {
                scaledImage.Save(ms, ImageFormat.Png);
                resizedImage = ms.ToArray();
            }
            return resizedImage;
        }
    }
}