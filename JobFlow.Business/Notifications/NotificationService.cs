using JobFlow.Business.DI;
using JobFlow.Business.Models;
using JobFlow.Business.Notifications.Builders;
using JobFlow.Business.Notifications.Models;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Notifications;

[ScopedService]
public class NotificationService : INotificationService
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

    public Task SendOrganizationSubsciptionPaymentFailedNotificationAsync(Organization organization)
    {
        throw new NotImplementedException();
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

    public async Task SendOrganizationSubscriptionPaymentFailedNotificationAsync(Organization org)
    {
        var message = _builder.BuildOrganizationSubscriptionFailed(org);
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
                Message = message.Body,
                TemplateId = (int)message.TemplateId,
                Link = message.Link
            });

            await _smsService.SendTextMessage(new TwilioModel
            {
                RecipientPhoneNumber = message.Phone,
                Message = message.Sms + message.Link
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending notification '{Subject}' to {Recipient} <{Email}>",
                message.Subject, message.Name, message.Email);
        }
    }
}