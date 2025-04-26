using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface INotificationService
    {
        // Organization notifications
        Task SendOrganizationWelcomeNotificationAsync(Organization organization);
        Task SendOrganizationSubsciptionPaymentFailedNotificationAsync(Organization organization);
        Task SendOrganizationPaymentReceivedNotificationAsync(Organization organization);

        // Client notifications
        Task SendClientWelcomeNotificationAsync(OrganizationClient client);
        Task SendClientJobCreatedNotificationAsync(OrganizationClient client, Job job);
        Task SendClientJobScheduledNotificationAsync(OrganizationClient client, Job job);
        Task SendClientInvoiceCreatedNotificationAsync(OrganizationClient client, Invoice invoice);
        Task SendClientPaymentReceivedNotificationAsync(OrganizationClient client, Invoice invoice);
    }
}
