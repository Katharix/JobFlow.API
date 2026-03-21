using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IInvoiceRealtimeNotifier
{
    Task NotifyInvoicePaidAsync(Invoice invoice);
}