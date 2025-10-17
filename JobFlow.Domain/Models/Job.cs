using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class Job : Entity
    {
        public Guid JobStatusId { get; set; }
        public virtual JobStatus JobStatus { get; set; }
        
        public string? Title { get; set; }
        public string? Comments { get; set; }

        public DateTime ScheduledStart { get; set; }
        public DateTime? ScheduledEnd { get; set; }
        public Guid OrganizationClientId { get; set; }
        public virtual OrganizationClient OrganizationClient { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Optional address fields
        public string? Address1 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }

        public virtual ICollection<JobOrder> JobOrders { get; set; } = new List<JobOrder>();
        public virtual ICollection<JobTracking> JobTrackings { get; set; } = new List<JobTracking>();
    }

}
