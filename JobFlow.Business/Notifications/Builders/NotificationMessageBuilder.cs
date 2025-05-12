using JobFlow.Business.DI;
using JobFlow.Business.Notifications.Models;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Notifications.Builders
{
    [ScopedService]
    public class NotificationMessageBuilder : INotificationMessageBuilder
    {
        public NotificationMessage BuildOrganizationWelcome(Organization org)
           => new NotificationMessage
           {
               Name = org.OrganizationName,
               Email = org.EmailAddress,
               Phone = org.PhoneNumber,
               Subject = "Welcome to JobFlow!",
               TemplateId = 2,
               Body = $"Hello {org.OrganizationName}, welcome aboard! We're excited to have you.",
               Sms = $"Welcome to Job Flow, {org.OrganizationName}!"
           };

        public NotificationMessage BuildOrganizationSubscriptionFailed(Organization org)
            => new NotificationMessage
            {
                Name = org.OrganizationName,
                Email = org.EmailAddress,
                Phone = org.PhoneNumber,
                Subject = "Subscription Payment Failed",
                Body = $"Hello {org.OrganizationName}, your subscription renewal payment has failed. Please update your payment method.",
                Sms = "Subscription payment failed. Please update your payment method."
            };

        public NotificationMessage BuildOrganizationPaymentReceived(Organization org)
            => new NotificationMessage
            {
                Name = org.OrganizationName,
                Email = org.EmailAddress,
                Phone = org.PhoneNumber,
                Subject = "Payment Received",
                Body = $"Hello {org.OrganizationName}, we have received your payment. Thank you!",
                Sms = "Payment received. Thank you!"
            };

        public NotificationMessage BuildClientWelcome(OrganizationClient client)
            => new NotificationMessage
            {
                Name = client.ClientFullName(),
                Email = client.EmailAddress,
                Phone = client.PhoneNumber,
                Subject = "Welcome to JobFlow!",
                Body = $"Hello {client.ClientFullName()}, welcome aboard!",
                Sms = $"Welcome {client.ClientFullName()}!"
            };

        public NotificationMessage BuildClientJobCreated(OrganizationClient client, Job job)
            => new NotificationMessage
            {
                Name = client.ClientFullName(),
                Email = client.EmailAddress,
                Phone = client.PhoneNumber,
                Subject = $"{client.Organization.OrganizationName} scheduled an appointment for you.",
                Body = $"Your appointment was scheduled for {job.ScheduledDate:MMMM dd, yyyy}.",
                Sms = $"Appointment scheduled for {job.ScheduledDate:MM/dd/yyyy}."
            };

        public NotificationMessage BuildClientJobScheduled(OrganizationClient client, Job job)
            => new NotificationMessage
            {
                Name = client.ClientFullName(),
                Email = client.EmailAddress,
                Phone = client.PhoneNumber,
                Subject = $"Appointment Scheduled",
                Body = $"Reminder: your appointment is scheduled for {job.ScheduledDate:MMMM dd, yyyy} at {job.ScheduledDate:hh:mm tt}.",
                Sms = $"Reminder: appointment on {job.ScheduledDate:MM/dd/yyyy}."
            };

        public NotificationMessage BuildClientInvoiceCreated(OrganizationClient client, Invoice invoice)
            => new NotificationMessage
            {
                Name = client.ClientFullName(),
                Email = client.EmailAddress,
                Phone = client.PhoneNumber,
                Subject = $"Invoice Created: #{invoice.Id}",
                Body = $"Your invoice #{invoice.Id} for {invoice.TotalAmount:C} is ready.",
                Sms = $"Invoice #{invoice.Id} ready: {invoice.TotalAmount:C}.",
                TemplateId = 3,
                Link = ""
            };

        public NotificationMessage BuildClientPaymentReceived(OrganizationClient client, Invoice invoice)
            => new NotificationMessage
            {
                Name = client.ClientFullName(),
                Email = client.EmailAddress,
                Phone = client.PhoneNumber,
                Subject = $"Payment Received: #{invoice.Id}",
                Body = $"We have received your payment for invoice #{invoice.Id}. Thank you!",
                Sms = $"Payment received for invoice #{invoice.Id}."
            };
    }
}
