using JobFlow.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobFlow.Domain.Models
{
    public class AssignmentHistory : Entity
    {
        public Guid AssignmentId { get; set; }
        public virtual Assignment Assignment { get; set; } = null!;

        public AssignmentEventType EventType { get; set; }

        public DateTime? OldScheduledStart { get; set; }
        public DateTime? NewScheduledStart { get; set; }

        public DateTime? OldScheduledEnd { get; set; }
        public DateTime? NewScheduledEnd { get; set; }

        public RescheduleReason? Reason { get; set; }
        public string? Notes { get; set; }

        public string? ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
