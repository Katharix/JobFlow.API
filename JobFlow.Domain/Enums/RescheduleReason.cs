using System;
using System.Collections.Generic;
using System.Text;

namespace JobFlow.Domain.Enums
{
    public enum RescheduleReason
    {
        Weather = 1,
        CustomerRequest = 2,
        CrewUnavailable = 3,
        EquipmentIssue = 4,
        AccessIssue = 5,
        Other = 99
    }
}
