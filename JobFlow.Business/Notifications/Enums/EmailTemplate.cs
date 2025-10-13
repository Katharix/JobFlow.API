using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Notifications.Enums
{
    public enum EmailTemplate
    {
        Default = 0,
        OrganizationWelcome = 2,
        InvoiceCreated = 3,
        OnTheWayNotification = 4,
        ArrivalNotification = 5
    }
}
