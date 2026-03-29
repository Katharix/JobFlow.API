using JobFlow.API.Hubs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.SignalR;

namespace JobFlow.API.Services;

public class InvoiceRealtimeNotifier : IInvoiceRealtimeNotifier
{
    private readonly IHubContext<NotifierHub> _notifierHub;
    private readonly IHubContext<ClientPortalHub> _clientHub;

    public InvoiceRealtimeNotifier(
        IHubContext<NotifierHub> notifierHub,
        IHubContext<ClientPortalHub> clientHub)
    {
        _notifierHub = notifierHub;
        _clientHub = clientHub;
    }

    public async Task NotifyInvoicePaidAsync(Invoice invoice)
    {
        var payload = new
        {
            invoiceId = invoice.Id,
            organizationId = invoice.OrganizationId,
            organizationClientId = invoice.OrganizationClientId,
            status = invoice.Status,
            balanceDue = invoice.BalanceDue,
            amountPaid = invoice.AmountPaid,
            paidAt = invoice.PaidAt
        };

        await _notifierHub.Clients.Group($"org:{invoice.OrganizationId}:dashboard")
            .SendAsync("InvoicePaid", payload);

        await _clientHub.Clients.Group($"client:{invoice.OrganizationClientId}")
            .SendAsync("InvoicePaid", payload);
    }
}