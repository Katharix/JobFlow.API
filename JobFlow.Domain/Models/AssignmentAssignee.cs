using System;
using System.Collections.Generic;
using System.Text;

namespace JobFlow.Domain.Models
{
    public class AssignmentAssignee
    {
        public Guid AssignmentId { get; set; }
        public virtual Assignment Assignment { get; set; }

        public Guid EmployeeId { get; set; } // assuming you already have Employee entity
        public virtual Employee Employee { get; set; }
        public bool IsLead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
