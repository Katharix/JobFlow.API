using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

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
    Task SendClientJobTrackingEtaNotificationAsync(OrganizationClient client, Job job, int etaMinutes);
    Task SendClientJobTrackingArrivalNotificationAsync(OrganizationClient client, Job job);
    Task SendClientEstimateSentNotificationAsync(OrganizationClient client, Estimate estimate);

    // Employee notifications
    Task SendEmployeeInviteNotificationAsync(EmployeeInvite invite);
}