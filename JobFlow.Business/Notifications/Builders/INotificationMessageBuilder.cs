using JobFlow.Business.Notifications.Models;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Notifications.Builders;

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

    NotificationMessage BuildClientJobTrackingEta(OrganizationClient client, Job job, int etaMinutes);
    NotificationMessage BuildClientJobTrackingArrival(OrganizationClient client, Job job);

    NotificationMessage BuildEmployeeInvite(EmployeeInvite invite);

    NotificationMessage BuildClientEstimateSent(OrganizationClient client, Estimate estimate);

    NotificationMessage BuildOrganizationClientPortalMagicLink(OrganizationClient client, string magicLink);
}