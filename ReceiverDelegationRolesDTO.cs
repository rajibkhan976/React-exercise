using Bluegarden.HRPlus.DTO;

namespace Bluegarden.HRPlus.ServiceLayer.DTO.Common
{
    public class ReceiverDelegationRolesDTO : BaseDTO
    {
        public int ReceiverRoleId { get; set; }
        public string RoleName { get; set; }
        public int[] ReceiverDelegationRoleIds { get; set; }
    }
}
