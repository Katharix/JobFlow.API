using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class Job
    {
        public Guid Id { get; set; }
        public Guid JobStatusId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public DateTime ScheduledTime { get; set; }
        public string Comments { get; set; }

        public virtual JobStatus JobStatus { get; set; }
        public virtual ICollection<OrganizationClientJob> OrganizationClientJobs { get; set; }
    }
}
