using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IInvoiceService
{
    Task<Result<Invoice>> GetInvoiceByIdAsync(Guid id);
    Task<Result<IEnumerable<Invoice>>> GetInvoicesByClientAsync(Guid clientId);
    Task<Result<Invoice>> UpsertInvoiceAsync(Invoice model);
    Task<Result> DeleteInvoiceAsync(Guid id);
    Task MarkInvoiceSentAsync(Guid invoiceId);

    Task<Result<Invoice>> MarkPaidAsync(
        Guid invoiceId,
        PaymentProvider provider,
        string externalPaymentId,
        decimal amountReceived);
}