using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IInvoiceLineItemService
{
    Task<Result<IEnumerable<InvoiceLineItem>>> GetByInvoiceIdAsync(Guid invoiceId);
    Task<Result> DeleteByInvoiceIdAsync(Guid invoiceId);
}