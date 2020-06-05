using System;
using System.Collections.Generic;
using System.Linq;
using Bluegarden.HRPlus.ServiceLayer.DTO.Common;
using Bluegarden.HRPlus.Common.Entities;
using Bluegarden.HRPlus.Common.Extensions;
using AutoMapper;
using Bluegarden.HRPlus.Integration;
using System.Data.Entity;
using Bluegarden.HRPlus.Models;
using Bluegarden.HRPlus.Permission;

namespace Bluegarden.HRPlus.ServiceLayer.Implementation
{
    public class DelegationService : IDelegationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserRoleService _userRoleService;
        private readonly IRoleService _roleService;

        public DelegationService(IUnitOfWork unitOfWork, IUserRoleService userRoleService, IRoleService roleService)
        {
            _unitOfWork = unitOfWork;
            _userRoleService = userRoleService;
            _roleService = roleService;
        }

        public List<DelegationDTO> GetAll(int userId, PagingQueryParams pagingQueryParams, out int total)
        {
            var query = _unitOfWork.DelegationRepository.AsQueryable()
                                   .Include(delegation => delegation.DestinationUserRole)
                                   .Include(delegation => delegation.DestinationUserRole.Role)
                                   .Include(delegation => delegation.DestinationUserRole.User)
                                   .Where(delegation => (delegation.SourceUserRole.StartDate == null || delegation.SourceUserRole.StartDate <= DateTime.Today) &&
                                                        (delegation.SourceUserRole.EndDate == null || delegation.SourceUserRole.EndDate >= DateTime.Today) &&
                                                        delegation.SourceUserRole.UserId == userId);

            var result = query.ApplyPaging(pagingQueryParams, out total).ToList();

            return Mapper.Map<List<DelegationDTO>>(result);
        }

        public DelegationDTO Get(int id)
        {
            return Mapper.Map<DelegationDTO>(_unitOfWork.DelegationRepository.AsQueryable()
                                                .Include(delegation => delegation.DestinationUserRole)
                                                .Include(delegation => delegation.SourceUserRole)
                                                .FirstOrDefault(delegation => delegation.Id == id));
        }

        public bool IsDelegatedRole(int destinationUserRoleId)
        {
            bool isDelegatedRole = false;
            var delegation = _unitOfWork.DelegationRepository.AsQueryable().FirstOrDefault(delg => delg.DestinationUserRoleId == destinationUserRoleId);
            if (delegation != null)
            {
                isDelegatedRole = true;
            }
            return isDelegatedRole;
        }

        public List<RoleDto> GetDelegableRolesByRoleId(int roleId)
        {
            var delegableRolesIds = _unitOfWork.DelegationRolesRepository.AsQueryable().Where(dr => dr.RoleId == roleId)
                                                                              .Select(dr => dr.DelegationRoleId)
                                                                              .ToArray();
            List<RoleDto> delegableRoles = new List<RoleDto>();

            foreach (var delegableRolesId in delegableRolesIds)
            {
                var delegableRole = _roleService.GetRoleById(delegableRolesId);
                delegableRoles.Add(delegableRole);
            }
            return delegableRoles;
        }

        public void Save(DelegationDTO delegationDTO, List<int> groupsIds)
        {
            var delegation = Mapper.Map<Delegation>(delegationDTO);

            if (delegationDTO.IsNew)
            {
                _unitOfWork.DelegationRepository.Add(delegation);
                _unitOfWork.UserRoleRepository.Add(delegation.DestinationUserRole);

                foreach (var groupId in groupsIds)
                {
                    _unitOfWork.UserRoleGroupRepository.Add(new UserRoleGroup { UserRole = delegation.DestinationUserRole, GroupId = groupId });
                }
            }
            else
            {
                var destinationUserRole = _unitOfWork.DelegationRepository.AsQueryable()
                                            .Include(del => del.DestinationUserRole)
                                            .Include(del => del.DestinationUserRole.UserRoleGroups)
                                            .First(del => del.Id == delegation.Id)
                                            .DestinationUserRole;

                foreach (var destinationUserRoleGroup in destinationUserRole.UserRoleGroups.ToList())
                {
                    _unitOfWork.UserRoleGroupRepository.Delete(destinationUserRoleGroup);
                }

                destinationUserRole.StartDate = delegation.DestinationUserRole.StartDate;
                destinationUserRole.EndDate = delegation.DestinationUserRole.EndDate;

                _unitOfWork.UserRoleRepository.Update(destinationUserRole);

                foreach (var groupId in groupsIds)
                {
                    _unitOfWork.UserRoleGroupRepository.Add(new UserRoleGroup { UserRole = destinationUserRole, GroupId = groupId });
                }
            }

            _unitOfWork.Save();
        }

        public void Delete(int id) 
        {
            var delegation = _unitOfWork.DelegationRepository.AsQueryable().Include(del => del.DestinationUserRole).FirstOrDefault(item => item.Id == id);

            if (delegation != null)
            {
                foreach (var userRoleGroup in _unitOfWork.UserRoleGroupRepository.AsQueryable().Where(urg => urg.UserRoleId == delegation.DestinationUserRoleId))
                {
                    _unitOfWork.UserRoleGroupRepository.Delete(userRoleGroup);
                }

                _unitOfWork.UserRoleRepository.Delete(delegation.DestinationUserRole);
                _unitOfWork.DelegationRepository.Delete(delegation);

                _unitOfWork.Save();
            }
        }

        public bool HasDelegationRoles(int userId)
        {
            return _userRoleService.GetUserRolesByUserIdAndDate(userId, DateTime.Today).Any(ur => ur.IsDelegering);
        }
    }
}
