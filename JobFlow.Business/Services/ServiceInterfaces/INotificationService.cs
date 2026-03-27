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
    Task SendClientJobRescheduledNotificationAsync(
        OrganizationClient client,
        Job job,
        DateTimeOffset previousStart,
        DateTimeOffset? previousEnd,
        DateTimeOffset newStart,
        DateTimeOffset? newEnd);
    Task SendClientInvoiceCreatedNotificationAsync(OrganizationClient client, Invoice invoice, string? linkOverride = null);
    Task SendClientInvoiceReminderNotificationAsync(OrganizationClient client, Invoice invoice, string? linkOverride = null);
    Task SendClientPaymentReceivedNotificationAsync(OrganizationClient client, Invoice invoice);
    Task SendClientJobTrackingEtaNotificationAsync(OrganizationClient client, Job job, int etaMinutes);
    Task SendClientJobTrackingArrivalNotificationAsync(OrganizationClient client, Job job);
    Task SendClientEstimateSentNotificationAsync(OrganizationClient client, Estimate estimate);
    Task SendClientEstimateFollowUpNotificationAsync(OrganizationClient client, Estimate estimate, string message);
    Task SendOrganizationEstimateRevisionRequestedNotificationAsync(Organization organization, OrganizationClient client, Estimate estimate, string revisionMessage);
    Task SendOrganizationClientPortalMagicLinkAsync(OrganizationClient client, string magicLink);

    // Employee notifications
    Task SendEmployeeInviteNotificationAsync(EmployeeInvite invite);
}