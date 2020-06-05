using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Microsoft.Web.Http;
using Bluegarden.HRPlus.ServiceLayer;
using Bluegarden.HRPlus.Web.Models.MyAccount;
using Bluegarden.HRPlus.ServiceLayer.DTO.Common;
using Bluegarden.HRPlus.Web.ActionFilters;
using Bluegarden.HRPlus.Web.Helpers;
using Bluegarden.HRPlus.Common.Entities;
using Bluegarden.HRPlus.Common;
using Bluegarden.HRPlus.Common.Enums;
using Bluegarden.HRPlus.ServiceLayer.Helpers;
using Bluegarden.HRPlus.Web.AppCode.Helpers;
using Bluegarden.HRPlus.Web.AppCode.Attributes.WebApi;

namespace Bluegarden.HRPlus.Web.Controllers.Api
{
    [AuditActionFilter]
    [ApiVersion("1.0")]
    [RoutePrefix("api/v{version:apiVersion}/Delegations")]
    [Authorize]
    [ValidateAntiForgeryWebApiToken]
    public class DelegationsApiController : ApiController
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IDelegationService _delegationService;
        private readonly IRoleService _roleService;
        private readonly IAdminUserRoleService _adminUserRoleService;
        private readonly IGroupService _groupService;
        private readonly ICustomerSettingsService _customerSettingsService;
        private readonly IConnectionInfo _connectionInfo;
        private readonly AuditHelper _auditHelper;
        private readonly IAuditService _auditService;
        private readonly IUserService _userService;

        public DelegationsApiController(IAuthorizationService authorizationService, IDelegationService delegationService, IRoleService roleService,
                                     IAdminUserRoleService adminUserRoleService, IGroupService groupService, ICustomerSettingsService customerSettingsService,
                                     IConnectionInfo connectionInfo, IAuditService auditService, IUserService userService)
        {
            _authorizationService = authorizationService;
            _delegationService = delegationService;
            _roleService = roleService;
            _adminUserRoleService = adminUserRoleService;
            _groupService = groupService;
            _connectionInfo = connectionInfo;
            _customerSettingsService = customerSettingsService;
            _auditService = auditService;
            _auditHelper = new AuditHelper();
            _userService = userService;
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult FindDelegations([FromUri] string sort = "Id", string sortDir = "ASC")
        {
            var model = new MyDelegationsModel();
            int total;
            model.GridModel.Records = _delegationService.GetAll(_connectionInfo.UserId, PagingQueryParams.Create(null, model.GridModel.RowsPerPage, sort, sortDir), out total);
            model.GridModel.TotalRecordsCount = total;

            return Ok(model);
        }

        [HttpGet]
        [Route("{id}")]
        public IHttpActionResult GetDelegationDetails(int? id)
        {
            var roles = _adminUserRoleService.GetDelegableUserRoles(_connectionInfo.UserId).ToList();

            var delegation = id.HasValue ?
                                 _delegationService.Get(id.Value) :
                                 CreateEmptyDelegationModel(roles[0].Id);

            var model = new MyDelegationEditModel
            {
                Delegation = delegation,
                Roles = roles,
                Users = id == null ? null :
                                        new List<UserDto>
                                        {
                                            delegation.DestinationUserRole.User
                                        }
            };
            return Ok(model);
        }

        [HttpGet]
        [Route("findDelegableRoles")]
        public IHttpActionResult GetDelegableRoles([FromUri] int roleId)
        {
            return Ok(_delegationService.GetDelegableRolesByRoleId(roleId));
        }

        [HttpGet]
        [Route("getDependsOnSelectedRolePartial")]
        public IHttpActionResult GetDependsOnSelectedRolePartial([FromUri] int selectedUserRoleId)
        {
            return Ok(CreateEmptyDelegationModel(selectedUserRoleId));
        }

        [AuditActionFilter(DisableAudit = true)]
        [HttpGet]
        [Route("group-tree")]
        public IHttpActionResult FindGroupTree([FromUri] int selectedUserRoleId, int? destinationUserRoleId)
        {
            var sourceGroups = _authorizationService.GetAllowedGroups(selectedUserRoleId);
            var checkedGroups = destinationUserRoleId.HasValue && destinationUserRoleId.Value > 0 ? _authorizationService.GetAllowedGroups(destinationUserRoleId.Value) : sourceGroups;
            return Ok(_groupService.GetAllForTreeView().DelegableGroupItemsToTreeView(null, checkedGroups, sourceGroups, hideUnauthorizedNodes: _customerSettingsService.GetAuthorizationSettings().UseGroupBasedDelegation));
        }

        [AuditActionFilter(DisableAudit = true)]
        [HttpGet]
        [Route("findAllowedGroupTree")]
        public IHttpActionResult FindAllowedGroupTree([FromUri] int selectedUserRoleId)
        {
            var sourceGroups = _authorizationService.GetAllowedGroups(selectedUserRoleId);
            return Ok(_groupService.GetAllForTreeView().DelegableGroupItemsToTreeView(null, null, sourceGroups, hideUnauthorizedNodes: _customerSettingsService.GetAuthorizationSettings().UseGroupBasedDelegation));
        }

        [HttpGet]
        [Route("users")]
        public IHttpActionResult FindDelegationUsers([FromUri] int userRoleId, string searchTerm)
        {
            return Ok(new
            {
                Results = _authorizationService.GetDelegationUsers(userRoleId, searchTerm)
                                                           .Select(u => new
                                                           {
                                                               id = u.Id,
                                                               text = $"{u.Username} - {u.FullName}"
                                                           })
            });
        }

        [HttpGet]
        [Route("findDelegableUsers")]
        public IHttpActionResult GetDelegableUsers([FromUri] int roleId)
        {
            return Ok(_authorizationService.GetDelegableUserByRoleId(roleId));
        }

        [HttpGet]
        [Route("findDelegatedUser/{id:int}")]
        public IHttpActionResult FindDelegatedUser(int id)
        {
            var result = _authorizationService.GetDelegatedUserById(id);

            return Ok(result);
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult SaveDelegation([FromUri] int userRoleId, [FromBody] MyDelegationsModel myDelegations)
        {
            var sourceUserRole = _adminUserRoleService.GetById(userRoleId);
            var delegation = new DelegationDTO
            {
                Id = myDelegations.DelegationId,
                SourceUserRole = sourceUserRole,
                DestinationUserRole = new UserRoleDTO
                {
                    RoleId = sourceUserRole.RoleId,
                    UserId = myDelegations.UserId,
                    StartDate = myDelegations.StartDate,
                    EndDate = myDelegations.EndDate
                }
            };

            var newGroups = _groupService.GetByIds(myDelegations.GroupCheckedIds).Select(x => x.Description);

            IList<string> oldGroupsFixed = new List<string>();

            if (myDelegations.DelegationId != 0)
            {
                var existingDelegation = _delegationService.Get(myDelegations.DelegationId);
                var oldGroups = _authorizationService.GetAllowedGroups(existingDelegation.DestinationUserRole.Id);
                oldGroupsFixed = _groupService.GetByIds(oldGroups).Select(x => x.Description).ToList();
            }

            _delegationService.Save(delegation, myDelegations.GroupCheckedIds);

            var user = _userService.GetUserById(myDelegations.UserId);
            var role = _roleService.GetRoleByUserRoleId(userRoleId);
            _auditService.Log(AuditEntryDTOBuilder.Build(
                                                         myDelegations.DelegationId == 0 ? AuditAction.DelegationCreated : AuditAction.DelegationEdited,
                                                         _connectionInfo.UserId,
                                                         null,
                                                         myDelegations.DelegationId == 0 ? null : new[] { _connectionInfo.UserName, role.Name, user.Username, myDelegations.StartDate?.ToShortDateString() ?? "", myDelegations.EndDate?.ToShortDateString() ?? "", string.Join<string>(",", oldGroupsFixed) },
                                                         new[] { _connectionInfo.UserName, role.Name, user.Username, myDelegations.StartDate?.ToShortDateString() ?? "", myDelegations.EndDate?.ToShortDateString() ?? "", string.Join<string>(",", newGroups) },
                                                         _auditHelper.GetIdentity()));
            return Ok(delegation);
        }

        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult DeleteDelegation(int id)
        {
            var existingDelegation = _delegationService.Get(id);
            var oldGroups = _authorizationService.GetAllowedGroups(existingDelegation.DestinationUserRole.Id);
            var oldGroupsFixed = _groupService.GetByIds(oldGroups).Select(x => x.Description).ToList();

            _delegationService.Delete(id);

            _auditService.Log(AuditEntryDTOBuilder.Build(
                                                         AuditAction.DelegationDeleted,
                                                         _connectionInfo.UserId,
                                                         null,
                                                         new[] { _connectionInfo.UserName,
                                                                   existingDelegation.DestinationUserRole.Role.Name,
                                                                   existingDelegation.DestinationUserRole.User.Username,
                                                                   existingDelegation.DestinationUserRole.StartDate?.ToShortDateString() ?? "",
                                                                   existingDelegation.DestinationUserRole.EndDate?.ToShortDateString() ?? "",
                                                                   string.Join<string>(",", oldGroupsFixed) },
                                                         null,
                                                         _auditHelper.GetIdentity()));
            return Ok("Delegation removed successfully!");
        }

        private DelegationDTO CreateEmptyDelegationModel(int userRoleId)
        {
            var sourceUserRole = _adminUserRoleService.GetById(userRoleId);
            var model = new DelegationDTO
            {
                SourceUserRole = sourceUserRole,
                DestinationUserRole = new UserRoleDTO
                {
                    Role = new RoleDto { Id = sourceUserRole.RoleId },
                    StartDate = sourceUserRole.StartDate,
                    EndDate = sourceUserRole.EndDate
                }
            };
            return model;
        }

    }
}