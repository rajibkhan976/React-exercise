using Bluegarden.HRPlus.DTO;

namespace Bluegarden.HRPlus.ServiceLayer.DTO.Common
{
    public class DelegationRolesDTO : BaseDTO
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public int[] DelegationRoleIds { get; set; }
    }
}
