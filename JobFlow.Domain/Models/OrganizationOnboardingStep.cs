using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class OrganizationOnboardingStep : Entity
    {
        public Guid OrganizationId { get; set; }
        public string StepName { get; set; }
        public bool IsCompleted { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }

        // Navigation
        public Organization Organization { get; set; }
    }
}
