using JobFlow.Business.DI;
using JobFlow.Business.Models;
using JobFlow.Business.Notifications.Builders;
using JobFlow.Business.Notifications.Models;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Notifications;

[ScopedService]
public partial class NotificationService : INotificationService
{
    private readonly INotificationMessageBuilder _builder;
    private readonly IBrevoService _emailService;
    private readonly ILogger<NotificationService> _logger;
    private readonly ITwilioService _smsService;

    public NotificationService(
        IBrevoService emailService,
        ITwilioService smsService,
        INotificationMessageBuilder builder,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService;
        _smsService = smsService;
        _builder = builder;
        _logger = logger;
    }

    // Organization notifications
    public async Task SendOrganizationWelcomeNotificationAsync(Organization org)
    {
        var message = _builder.BuildOrganizationWelcome(org);
        await SendNotificationAsync(message);
    }

    public async Task SendOrganizationPaymentReceivedNotificationAsync(Organization org)
    {
        var message = _builder.BuildOrganizationPaymentReceived(org);
        await SendNotificationAsync(message);
    }

    public async Task SendOrganizationSubsciptionPaymentFailedNotificationAsync(Organization organization)
    {
        // Keep existing behavior (use builder + shared sender)
        var message = _builder.BuildOrganizationSubscriptionFailed(organization);
        await SendNotificationAsync(message);
    }

    // Client notifications
    public async Task SendClientWelcomeNotificationAsync(OrganizationClient client)
    {
        var message = _builder.BuildClientWelcome(client);
        await SendNotificationAsync(message);
    }

    public async Task SendClientJobCreatedNotificationAsync(OrganizationClient client, Job job)
    {
        var message = _builder.BuildClientJobCreated(client, job);
        await SendNotificationAsync(message);
    }

    public async Task SendClientJobScheduledNotificationAsync(OrganizationClient client, Job job)
    {
        var message = _builder.BuildClientJobScheduled(client, job);
        await SendNotificationAsync(message);
    }

    public async Task SendClientJobRescheduledNotificationAsync(
        OrganizationClient client,
        Job job,
        DateTimeOffset previousStart,
        DateTimeOffset? previousEnd,
        DateTimeOffset newStart,
        DateTimeOffset? newEnd)
    {
        var message = _builder.BuildClientJobRescheduled(client, job, previousStart, previousEnd, newStart, newEnd);
        await SendNotificationAsync(message);
    }

    public async Task SendClientInvoiceCreatedNotificationAsync(OrganizationClient client, Invoice invoice)
    {
        var message = _builder.BuildClientInvoiceCreated(client, invoice);
        await SendNotificationAsync(message);
    }

    public async Task SendClientPaymentReceivedNotificationAsync(OrganizationClient client, Invoice invoice)
    {
        var message = _builder.BuildClientPaymentReceived(client, invoice);
        await SendNotificationAsync(message);
    }

    public async Task SendClientJobTrackingEtaNotificationAsync(OrganizationClient client, Job job, int etaMinutes)
    {
        var message = _builder.BuildClientJobTrackingEta(client, job, etaMinutes);
        await SendNotificationAsync(message);
    }

    public async Task SendClientJobTrackingArrivalNotificationAsync(OrganizationClient client, Job job)
    {
        var message = _builder.BuildClientJobTrackingArrival(client, job);
        await SendNotificationAsync(message);
    }

    public async Task SendEmployeeInviteNotificationAsync(EmployeeInvite invite)
    {
        var message = _builder.BuildEmployeeInvite(invite);
        await SendNotificationAsync(message);
    }

    public async Task SendOrganizationClientPortalMagicLinkAsync(OrganizationClient client, string magicLink)
    {
        var message = _builder.BuildOrganizationClientPortalMagicLink(client, magicLink);
        await SendNotificationAsync(message);
    }

    public async Task SendOrganizationSubscriptionPaymentFailedNotificationAsync(Organization org)
    {
        var message = _builder.BuildOrganizationSubscriptionFailed(org);
        await SendNotificationAsync(message);
    }

    public async Task SendOrganizationEstimateRevisionRequestedNotificationAsync(
        Organization organization,
        OrganizationClient client,
        Estimate estimate,
        string revisionMessage)
    {
        var message = _builder.BuildOrganizationEstimateRevisionRequested(organization, client, estimate, revisionMessage);
        await SendNotificationAsync(message);
    }

    /// <summary>
    ///     Shared helper for sending email and SMS.
    /// </summary>
    private async Task SendNotificationAsync(NotificationMessage message)
    {
        try
        {
            await _emailService.SendContactEmailAsync(new ContactFormRequest
            {
                Email = message.Email,
                Name = message.Name,
                Subject = message.Subject,
                Message = message.Body,
                TemplateId = (int)message.TemplateId,
                Link = message.Link
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email failed for {Recipient} <{Email}>", message.Name, message.Email);
        }

        try
        {
            await _smsService.SendTextMessage(new TwilioModel
            {
                RecipientPhoneNumber = message.Phone,
                Message = message.Sms + message.Link
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS failed for {Recipient} <{Phone}>", message.Name, message.Phone);
        }
    }
}