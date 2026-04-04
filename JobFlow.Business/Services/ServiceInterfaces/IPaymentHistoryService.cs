using JobFlow.Domain.Models;
using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IPaymentHistoryService
{
    Task<Result> LogAsync(PaymentHistory history);
    Task<Result<List<PaymentHistory>>> GetPaymentsForEntityAsync(Guid entityId);
    Task<Result<CursorPagedResponseDto<PaymentEventListItemDto>>> GetPaymentEventsForEntityAsync(
        Guid entityId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageSize,
        string? cursor,
        bool disputesOnly);
    Task<Result<PaymentHistoryAggregateDto>> GetFinancialAggregatesAsync(Guid entityId, DateTime monthStartUtc);
}