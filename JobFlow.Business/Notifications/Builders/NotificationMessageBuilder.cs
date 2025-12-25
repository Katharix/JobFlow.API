using JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces;
using JobFlow.Business.DI;
using JobFlow.Business.Notifications.Enums;
using JobFlow.Business.Notifications.Models;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Notifications.Builders;

[ScopedService]
public class NotificationMessageBuilder : INotificationMessageBuilder
{
    private readonly IFrontendSettings _backendSettings;
    private readonly string baseUrl;

    public NotificationMessageBuilder(IFrontendSettings backendSettings)
    {
        _backendSettings = backendSettings;
        baseUrl = _backendSettings.BaseUrl;
    }

    public NotificationMessage BuildOrganizationWelcome(Organization org)
    {
        return new NotificationMessage
        {
            Name = org.OrganizationName,
            Email = org.EmailAddress,
            Phone = org.PhoneNumber,
            Subject = "Welcome to JobFlow!",
            TemplateId = EmailTemplate.OrganizationWelcome,
            Body = $"Hello {org.OrganizationName}, welcome aboard! We're excited to have you.",
            Sms = $"Welcome to Job Flow, {org.OrganizationName}!"
        };
    }

    public NotificationMessage BuildOrganizationSubscriptionFailed(Organization org)
    {
        return new NotificationMessage
        {
            Name = org.OrganizationName,
            Email = org.EmailAddress,
            Phone = org.PhoneNumber,
            Subject = "Subscription Payment Failed",
            Body =
                $"Hello {org.OrganizationName}, your subscription renewal payment has failed. Please update your payment method.",
            Sms = "Subscription payment failed. Please update your payment method."
        };
    }

    public NotificationMessage BuildOrganizationPaymentReceived(Organization org)
    {
        return new NotificationMessage
        {
            Name = org.OrganizationName,
            Email = org.EmailAddress,
            Phone = org.PhoneNumber,
            Subject = "Payment Received",
            Body = $"Hello {org.OrganizationName}, we have received your payment. Thank you!",
            Sms = "Payment received. Thank you!"
        };
    }

    public NotificationMessage BuildClientWelcome(OrganizationClient client)
    {
        return new NotificationMessage
        {
            Name = client.ClientFullName(),
            Email = client.EmailAddress,
            Phone = client.PhoneNumber,
            Subject = "Welcome to JobFlow!",
            Body = $"Hello {client.ClientFullName()}, welcome aboard!",
            Sms = $"Welcome {client.ClientFullName()}!"
        };
    }

    public NotificationMessage BuildClientJobCreated(OrganizationClient client, Job job)
    {
        return new NotificationMessage
        {
            Name = client.ClientFullName(),
            Email = client.EmailAddress,
            Phone = client.PhoneNumber,
            Subject = $"{client.Organization.OrganizationName} scheduled an appointment for you.",
            Body = $"Your appointment was scheduled for {job.ScheduledStart:MMMM dd, yyyy}.",
            Sms = $"Appointment scheduled for {job.ScheduledStart:MM/dd/yyyy}."
        };
    }

    public NotificationMessage BuildClientJobScheduled(OrganizationClient client, Job job)
    {
        return new NotificationMessage
        {
            Name = client.ClientFullName(),
            Email = client.EmailAddress,
            Phone = client.PhoneNumber,
            Subject = "Appointment Scheduled",
            Body =
                $"Reminder: your appointment is scheduled for {job.ScheduledStart:MMMM dd, yyyy} at {job.ScheduledStart:hh:mm tt}.",
            Sms = $"Reminder: appointment on {job.ScheduledStart:MM/dd/yyyy}."
        };
    }

    public NotificationMessage BuildClientInvoiceCreated(OrganizationClient client, Invoice invoice)
    {
        return new NotificationMessage
        {
            Name = client.ClientFullName(),
            Email = client.EmailAddress,
            Phone = client.PhoneNumber,
            Subject = $"Invoice Created: #{invoice.Id}",
            Body = $"Your invoice #{invoice.Id} for {invoice.TotalAmount:C} is ready.",
            Sms = $"Invoice #{invoice.Id} ready: {invoice.TotalAmount:C}.",
            TemplateId = EmailTemplate.InvoiceCreated,
            Link = $"{baseUrl}/invoice/view/{invoice.Id}"
        };
    }

    public NotificationMessage BuildClientPaymentReceived(OrganizationClient client, Invoice invoice)
    {
        return new NotificationMessage
        {
            Name = client.ClientFullName(),
            Email = client.EmailAddress,
            Phone = client.PhoneNumber,
            Subject = $"Payment Received: #{invoice.Id}",
            Body = $"We have received your payment for invoice #{invoice.Id}. Thank you!",
            Sms = $"Payment received for invoice #{invoice.Id}."
        };
    }

    public NotificationMessage BuildClientJobTrackingEta(OrganizationClient client, Job job, int etaMinutes)
    {
        return new NotificationMessage
        {
            Email = client.EmailAddress,
            Phone = client.PhoneNumber,
            Name = client.ClientFullName(),
            Subject = $"Your worker is on the way for {job.Title}",
            Body =
                $"Hello {client.ClientFullName()},\n\nYour JobFlow worker is about {etaMinutes} minutes away for your job: {job.Title}.",
            Sms = $"Your JobFlow worker is about {etaMinutes} minutes away for {job.Title}. ",
            TemplateId = EmailTemplate.Default
        };
    }

    public NotificationMessage BuildClientJobTrackingArrival(OrganizationClient client, Job job)
    {
        return new NotificationMessage
        {
            Email = client.EmailAddress,
            Phone = client.PhoneNumber,
            Name = client.ClientFullName(),
            Subject = $"Your worker has arrived for {job.Title}",
            Body =
                $"Hello {client.ClientFullName()},\n\nYour JobFlow worker has arrived at your location for job: {job.Title}.",
            Sms = $"Your JobFlow worker has arrived for {job.Title}. ",
            TemplateId = EmailTemplate.Default
        };
    }

    public NotificationMessage BuildEmployeeInvite(EmployeeInvite invite)
    {
        var link = $"{baseUrl}/i/{invite.ShortCode}";
        return new NotificationMessage
        {
            Email = invite.Email,
            Name = invite.FullName,
            Phone = invite.PhoneNumber,
            Subject = $"You're invited to join {invite.Organization?.OrganizationName ?? "JobFlow"}",
            Body = $"""
                        Hello {invite.FullName},

                        You’ve been invited to join {invite.Organization?.OrganizationName ?? "JobFlow"}.
                        Click below to accept your invitation:
                        {link}

                        This link will expire on {invite.ExpiresAt:MMM dd, yyyy}.
                    """,
            Sms =
                $"You’ve been invited to join {invite.Organization?.OrganizationName ?? "JobFlow"}! Accept your invite: ",
            Link = $"{link}",
            TemplateId = EmailTemplate.OrganizationWelcome
        };
    }
}