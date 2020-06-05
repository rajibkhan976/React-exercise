using System.ComponentModel.DataAnnotations.Schema;
using Bluegarden.HRPlus.Models.BaseEntities;

namespace Bluegarden.HRPlus.Models
{
    public class DelegationRoles : BaseEntity
    {
        public int RoleId { get; set; }

        public int DelegationRoleId { get; set; }

        public virtual Role Role { get; set; }

        public virtual Role DelegationRole { get; set; }
    }
}
