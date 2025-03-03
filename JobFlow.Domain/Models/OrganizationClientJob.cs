using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class OrganizationClientJob
    {
        public Guid JobId { get; set; }
        public Guid OrganizationClientId { get; set; }
        public DateTime CreatedDate { get; set; }
        public virtual Job Job { get; set; }
        public virtual OrganizationClient OrganizationClient { get; set; }
    }
}
