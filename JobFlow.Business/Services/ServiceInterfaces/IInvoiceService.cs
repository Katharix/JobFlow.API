using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IInvoiceService
{
    Task<Result<Invoice>> GetInvoiceByIdAsync(Guid id);
    Task<Result<IEnumerable<Invoice>>> GetInvoicesByClientAsync(Guid clientId);
    Task<Result<Invoice>> UpsertInvoiceAsync(Invoice model);
    Task<Result> DeleteInvoiceAsync(Guid id);
    Task MarkInvoiceSentAsync(Guid invoiceId);
}