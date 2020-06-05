using System;
using System.Collections.Generic;
using System.Linq;
using Bluegarden.HRPlus.Common.Constants;
using Bluegarden.HRPlus.Common.Enums;
using Bluegarden.HRPlus.Integration;
using Bluegarden.HRPlus.Models;
using System.Data.Entity;
using Bluegarden.HRPlus.ServiceLayer.DTO.Common;
using AutoMapper;
using Bluegarden.HRPlus.Common;
using Bluegarden.HRPlus.Common.Exceptions;
using Bluegarden.HRPlus.Common.Localization;

namespace Bluegarden.HRPlus.ServiceLayer.Implementation
{
    public class RoleService : IRoleService
    {
        readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly IConnectionInfo _connectionInfo;

        public RoleService(IUnitOfWork unitOfWork, IAuditService auditService, IConnectionInfo connectionInfo)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _connectionInfo = connectionInfo;
        }

        public IEnumerable<RoleDto> GetRoles()
        {
            return Mapper.Map<List<RoleDto>>(_unitOfWork.RoleRepository
                .AsQueryable().OrderBy(x => x.Name).Include(x => x.RolePermissions));
        }

        public IList<int> GetDelegationRoles(int roleId)
        {
            return _unitOfWork.DelegationRolesRepository.AsQueryable().Where(dr => dr.RoleId == roleId)
                                                                              .Select(dr => dr.DelegationRoleId)
                                                                              .ToList();
        }

        public IList<int> GetReceiverDelegationRoles(int roleId)
        {
            return _unitOfWork.ReceiverDelegationRolesRepository.AsQueryable().Where(rd => rd.ReceiverRoleId == roleId)
                                                                              .Select(rd => rd.ReceiverDelegationRoleId)
                                                                              .ToList();
        }

        public bool DeleteRole(int roleId)
        {
            var isRoleConnectedToAnyUser = _unitOfWork.UserRoleRepository
                .AsQueryable().Any(ur => roleId == ur.RoleId);

            if (isRoleConnectedToAnyUser)
            {
                throw new HRPlusBaseException(LocalizationHelper.Instance.GetTranslation(LocalizationKeys.Administration.Authorization.Role.UsersConnectedToRole));
            }

            var isRoleConnectedToAnyLinkGroups = _unitOfWork.LinkGroupRepository
                                                            .AsQueryable().Any(lg => roleId == lg.RoleId);

            if (isRoleConnectedToAnyLinkGroups)
            {
                throw new HRPlusBaseException(LocalizationHelper.Instance.GetTranslation(LocalizationKeys.Administration.Authorization.Role.LinkGroupsConnectedToRole));
            }

            DeleteDelegationRoles(roleId);
            DeleteReceiverDelegationRoles(roleId);
            DeleteRolePermissions(roleId);
            DeleteCaseDefinitionRoleActions(roleId);

            var role = _unitOfWork.RoleRepository.AsQueryable().FirstOrDefault(item => item.Id == roleId);
            _unitOfWork.RoleRepository.Delete(role);
            _unitOfWork.Save();
            return true;
        }

        private void DeleteDelegationRoles(int roleId)
        {
            var delegationRoles = _unitOfWork.DelegationRolesRepository.AsQueryable().Where(dr => dr.RoleId == roleId);

            foreach (var delegationRole in delegationRoles)
            {
                _unitOfWork.DelegationRolesRepository.Delete(delegationRole);
            }
            _unitOfWork.Save();
        }

        private void DeleteReceiverDelegationRoles(int roleId)
        {
            var receiverDelegationRoles = _unitOfWork.ReceiverDelegationRolesRepository.AsQueryable().Where(rd => rd.ReceiverRoleId == roleId);

            foreach (var receiverDelegationRole in receiverDelegationRoles)
            {
                _unitOfWork.ReceiverDelegationRolesRepository.Delete(receiverDelegationRole);
            }
            _unitOfWork.Save();
        }

        private void DeleteCaseDefinitionRoleActions(int roleId)
        {
            var roleActions = _unitOfWork
                .CaseDefinitionRoleActionRepository.AsQueryable()
                .Where(rp => rp.RoleId == roleId);
            foreach (var ra in roleActions)
            {
                _unitOfWork.CaseDefinitionRoleActionRepository
                    .Delete(ra);
            }
        }

        private void DeleteRolePermissions(int roleId)
        {
            var rolePermissions = _unitOfWork.RolePermissionRepository.AsQueryable().Where(rp => rp.RoleId == roleId);
            foreach (var rp in rolePermissions)
            {
                _unitOfWork.RolePermissionRepository.Delete(rp);
            }
        }

        public void Save(RoleDto roleDto)
        {
            var role = Mapper.Map<Role>(roleDto);
            if (0 == role.Id)
            {
                _unitOfWork.RoleRepository.Add(role);  
            }
            else
            {
                _unitOfWork.RoleRepository.Update(role);
            }

            _unitOfWork.Save();
            roleDto.Id = role.Id;
        }

        public void SaveDelegationRoles(DelegationRolesDTO delegationRoles)
        {
            var newRoleId = _unitOfWork.RoleRepository.AsQueryable().Where(ur => (ur.Name == delegationRoles.RoleName)).Select(ur => ur.Id).ToList().LastOrDefault();

            foreach (var delegationRoleId in delegationRoles.DelegationRoleIds)
            {
                _unitOfWork.DelegationRolesRepository.Add(new DelegationRoles { RoleId = newRoleId, DelegationRoleId = delegationRoleId });
            }

            _unitOfWork.Save();
        }

        public void SaveReceiverDelegationRoles(ReceiverDelegationRolesDTO receiverDelegationRoles)
        {
            var newRoleId = _unitOfWork.RoleRepository.AsQueryable().Where(ur => (ur.Name == receiverDelegationRoles.RoleName)).Select(ur => ur.Id).ToList().LastOrDefault(); ;

            foreach (var receiverDelegationRoleId in receiverDelegationRoles.ReceiverDelegationRoleIds)
            {
                _unitOfWork.ReceiverDelegationRolesRepository.Add(new ReceiverDelegationRoles { ReceiverRoleId = newRoleId, ReceiverDelegationRoleId = receiverDelegationRoleId });
            }

            _unitOfWork.Save();
        }

        public void UpdateDelegationRoles(DelegationRolesDTO delegationRoles)
        {
            if (delegationRoles != null)
            {
                var delegableRoles = _unitOfWork.DelegationRolesRepository.AsQueryable().Where(dr => dr.RoleId == delegationRoles.RoleId)
                                                                              .Select(dr => dr.DelegationRoleId)
                                                                              .ToArray();

                if (delegableRoles != null && delegableRoles.Length != 0)
                {
                    DeleteDelegationRoles(delegationRoles.RoleId);

                    foreach (var delegationRoleId in delegationRoles.DelegationRoleIds)
                    {
                        {
                            _unitOfWork.DelegationRolesRepository.Add(new DelegationRoles { RoleId = delegationRoles.RoleId, DelegationRoleId = delegationRoleId });
                        }
                    }
                }
                else
                {
                    foreach (var delegationRoleId in delegationRoles.DelegationRoleIds)
                    {
                        _unitOfWork.DelegationRolesRepository.Add(new DelegationRoles { RoleId = delegationRoles.RoleId, DelegationRoleId = delegationRoleId });
                    }
                }
            }
            _unitOfWork.Save();
        }

        public void UpdateReceiverDelegationRoles(ReceiverDelegationRolesDTO receiverDelegationRoles)
        {
            if (receiverDelegationRoles != null)
            {
                var receiverDelegatedRoles = _unitOfWork.ReceiverDelegationRolesRepository.AsQueryable().Where(rd => rd.ReceiverRoleId == receiverDelegationRoles.ReceiverRoleId)
                                                                              .Select(rd => rd.ReceiverDelegationRoleId)
                                                                              .ToArray();

                if (receiverDelegatedRoles != null && receiverDelegatedRoles.Length != 0)
                {
                    DeleteReceiverDelegationRoles(receiverDelegationRoles.ReceiverRoleId);

                    foreach (var receiverDelegationRoleId in receiverDelegationRoles.ReceiverDelegationRoleIds)
                    {
                        {
                            _unitOfWork.ReceiverDelegationRolesRepository.Add(new ReceiverDelegationRoles { ReceiverRoleId = receiverDelegationRoles.ReceiverRoleId, ReceiverDelegationRoleId = receiverDelegationRoleId });
                        }
                    }
                }
                else
                {
                    foreach (var receiverDelegationRoleId in receiverDelegationRoles.ReceiverDelegationRoleIds)
                    {
                        _unitOfWork.ReceiverDelegationRolesRepository.Add(new ReceiverDelegationRoles { ReceiverRoleId = receiverDelegationRoles.ReceiverRoleId, ReceiverDelegationRoleId = receiverDelegationRoleId });
                    }
                }
            }
            _unitOfWork.Save();
        }

        public void SavePermissionsForRole(int roleId, List<RolePermissionDTO> rolePermissions, AuditIdentityDTO identity)
        {
            //RemoveAll is not implemented
            var allPermissions = _unitOfWork.RolePermissionRepository.AsQueryable().Where(rp => roleId == rp.RoleId);
            var oldPermissionIds = allPermissions.Select(x => x.PermissionId).ToList();
            foreach (var permission in allPermissions)
            {
                _unitOfWork.RolePermissionRepository.Delete(permission);
            }

            var newPermissionIds = rolePermissions.Select(x => x.PermissionId).ToList();
            if ( 0 < rolePermissions.Count)
            {
                foreach (var rolePermission in rolePermissions)
                {
                    var newPermission = Mapper.Map<RolePermission>(rolePermission);
                    if (newPermission.RoleId == 0)
                    {
                        newPermission.RoleId = roleId;
                    }
                    _unitOfWork.RolePermissionRepository.Add(newPermission);
                }
            }
            
            _unitOfWork.Save();
            LogPermissionsChange(roleId, newPermissionIds, oldPermissionIds, identity);
        }

        private void LogPermissionsChange(int roleId, IList<int> newPermissionIds, IList<int> oldPermissionIds, AuditIdentityDTO identity)
        {
            var role = _unitOfWork.RoleRepository.AsQueryable().First(x => x.Id == roleId);
            var oldPermissionNames = _unitOfWork.PermissionRepository.AsQueryable()
                    .Where(x => oldPermissionIds.Contains(x.Id))
                    .Select(x => x.Name).ToList();

            var newPermissionNames = _unitOfWork.PermissionRepository.AsQueryable()
                    .Where(x => newPermissionIds.Contains(x.Id))
                    .Select(x => x.Name);


            _auditService.Log(new AuditEntryDTO
            {
                UserId = _connectionInfo.UserId,
                Action = AuditAction.RolePermissionsChanged,
                Category = AuditCategory.Security,
                Level = AuditLevel.Information,
                OldValue = AuditValueMessage.RolePermissionsChangedOldValues,
                OldValueParams = new []{string.Join(", ", oldPermissionNames)},
                NewValue = AuditValueMessage.RolePermissionsChangedNewValues,
                NewValueParams = new[] { string.Join(", ", newPermissionNames) },
                Message = AuditMessage.RolePermissionsChanged,
                MessageParams = new[] { roleId.ToString(), role.Name },
                Identity = identity
            });
        }

        public RoleDto GetRoleById(int id)
        {
            return Mapper.Map<RoleDto>(_unitOfWork.RoleRepository.AsQueryable().FirstOrDefault(item => item.Id == id));
        }

        public RoleDto GetRoleByUserRoleId(int id)
        {
            var userRole = _unitOfWork.UserRoleRepository.AsQueryable().FirstOrDefault(item => item.Id == id);
            if (userRole != null)
            {
                return Mapper.Map<RoleDto>(_unitOfWork.RoleRepository.AsQueryable().FirstOrDefault(item => item.Id == userRole.RoleId));
            }
            return null;
        }

        public bool IsMainManagerRole(int roleId)
        {
            return _unitOfWork.RolePermissionRepository
                              .AsQueryable()
                              .Include(x => x.Permission)
                              .Any(x => x.Permission.Name.Equals(Permissions.IsMainManager) && x.RoleId == roleId);
        }

        public IList<int> GetRolesIdsByUserId(int userId)
        {
            return _unitOfWork.RoleRepository
                              .AsQueryable()
                              .Where(role => role.UserRoles.Any(userRole => userRole.UserId == userId))
                              .Select(r => r.Id)
                              .ToList();
        }
    }
}
