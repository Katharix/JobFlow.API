using JobFlow.Business.Notifications.Models;
using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Notifications.Builders
{
    public interface INotificationMessageBuilder
    {
        NotificationMessage BuildOrganizationWelcome(Organization org);
        NotificationMessage BuildOrganizationSubscriptionFailed(Organization org);
        NotificationMessage BuildOrganizationPaymentReceived(Organization org);

        NotificationMessage BuildClientWelcome(OrganizationClient client);
        NotificationMessage BuildClientJobCreated(OrganizationClient client, Job job);
        NotificationMessage BuildClientJobScheduled(OrganizationClient client, Job job);
        NotificationMessage BuildClientInvoiceCreated(OrganizationClient client, Invoice invoice);
        NotificationMessage BuildClientPaymentReceived(OrganizationClient client, Invoice invoice);
    }
}
