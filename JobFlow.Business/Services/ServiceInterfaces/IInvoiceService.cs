using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IInvoiceService
{
    Task<Result<Invoice>> GetInvoiceByIdAsync(Guid id);
    Task<Result<IEnumerable<Invoice>>> GetInvoicesByClientAsync(Guid clientId);
    Task<Result<IEnumerable<Invoice>>> GetInvoicesByOrganizationAsync(Guid organizationId);
    Task<Result<CursorPagedResponseDto<Invoice>>> GetInvoicesByOrganizationPagedAsync(
        Guid organizationId,
        int pageSize,
        string? cursor,
        string? statusFilter,
        string? search,
        string? sortBy,
        string? sortDirection);
    Task<Result<InvoiceAggregateDto>> GetInvoiceAggregatesByOrganizationAsync(Guid organizationId);
    Task<Result<Invoice>> UpsertInvoiceAsync(Invoice model);
    Task<Result> DeleteInvoiceAsync(Guid id);
    Task MarkInvoiceSentAsync(Guid invoiceId);
    Task<bool> IsPaidAsync(Guid invoiceId);
    Task<Result> SendInvoiceToClientAsync(Guid invoiceId);
    Task<Result> SendInvoiceForJobAsync(Guid organizationId, Job job);

    Task<Result<Invoice>> MarkPaidAsync(
        Guid invoiceId,
        PaymentProvider provider,
        string externalPaymentId,
        decimal amountReceived);

    Task<Result<Invoice>> RecordDepositAsync(
        Guid invoiceId,
        decimal depositAmount,
        PaymentProvider provider,
        string externalPaymentId);
}