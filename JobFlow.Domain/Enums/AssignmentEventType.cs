using System;
using System.Collections.Generic;
using System.Text;

namespace JobFlow.Domain.Enums
{
    public enum AssignmentEventType
    {
        None = 0,
        Created = 1,
        Rescheduled = 2,
        Cancelled = 3,
        Skilled = 4,
        Comleted = 5,
        Assigned = 6
        // Created, Rescheduled, Cancelled, Skipped, Completed, Assigned
    }
}
