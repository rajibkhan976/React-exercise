using System.ComponentModel.DataAnnotations.Schema;
using Bluegarden.HRPlus.Models.BaseEntities;

namespace Bluegarden.HRPlus.Models
{
    public class ReceiverDelegationRoles : BaseEntity
    {
        public int ReceiverRoleId { get; set; }

        public int ReceiverDelegationRoleId { get; set; }

        public virtual Role ReceiverRole { get; set; }

        public virtual Role ReceiverDelegationRole { get; set; }
    }
}
