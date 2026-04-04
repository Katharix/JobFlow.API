using JobFlow.Business.DI;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace JobFlow.Business.Services;

[ScopedService]
public class PaymentHistoryService : IPaymentHistoryService
{
    private readonly ILogger<PaymentHistoryService> logger;
    private readonly IRepository<PaymentHistory> paymentHistory;
    private readonly IUnitOfWork unitOfWork;

    public PaymentHistoryService(ILogger<PaymentHistoryService> logger, IUnitOfWork unitOfWork)
    {
        this.logger = logger;
        this.unitOfWork = unitOfWork;
        paymentHistory = unitOfWork.RepositoryOf<PaymentHistory>();
    }

    public async Task<Result> LogAsync(PaymentHistory history)
    {
        await paymentHistory.AddAsync(history);
        await unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<PaymentHistory>>> GetPaymentsForEntityAsync(Guid entityId)
    {
        var records = await paymentHistory.Query()
            .Where(p => p.EntityId == entityId)
            .ToListAsync();

        return Result<List<PaymentHistory>>.Success(records);
    }

    public async Task<Result<CursorPagedResponseDto<PaymentEventListItemDto>>> GetPaymentEventsForEntityAsync(
        Guid entityId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageSize,
        string? cursor,
        bool disputesOnly)
    {
        var size = Math.Clamp(pageSize, 1, 250);
        var query = paymentHistory.Query()
            .AsNoTracking()
            .Where(p => p.EntityId == entityId);

        if (fromUtc.HasValue)
            query = query.Where(p => p.PaidAt >= fromUtc.Value.ToUniversalTime());

        if (toUtc.HasValue)
            query = query.Where(p => p.PaidAt <= toUtc.Value.ToUniversalTime());

        if (disputesOnly)
            query = query.Where(p => EF.Functions.Like(p.EventType, "%dispute%"));

        if (TryReadCursor(cursor, out var cursorPaidAt, out _))
        {
            query = query.Where(p => p.PaidAt <= cursorPaidAt);
        }

        var batch = await query
            .OrderByDescending(p => p.PaidAt)
            .ThenByDescending(p => p.Id)
            .Select(p => new PaymentEventListItemDto
            {
                Id = p.Id,
                EventType = p.EventType,
                Status = p.Status,
                PaymentProvider = p.PaymentProvider,
                AmountPaid = p.AmountPaid,
                Currency = p.Currency,
                PaidAt = p.PaidAt,
                InvoiceId = p.InvoiceId,
                StripePaymentIntentId = p.StripePaymentIntentId,
                SubscriptionId = p.SubscriptionId,
                CustomerId = p.CustomerId
            })
            .Take(size + 25)
            .ToListAsync();

        if (TryReadCursor(cursor, out cursorPaidAt, out var cursorId))
        {
            batch = batch
                .Where(p => p.PaidAt < cursorPaidAt || (p.PaidAt == cursorPaidAt && p.Id.CompareTo(cursorId) < 0))
                .ToList();
        }

        var hasMore = batch.Count > size;
        var items = hasMore ? batch.Take(size).ToList() : batch;

        var nextCursor = hasMore && items.Count > 0
            ? BuildCursor(items[^1].PaidAt, items[^1].Id)
            : null;

        return Result<CursorPagedResponseDto<PaymentEventListItemDto>>.Success(new CursorPagedResponseDto<PaymentEventListItemDto>
        {
            Items = items,
            NextCursor = nextCursor
        });
    }

    public async Task<Result<PaymentHistoryAggregateDto>> GetFinancialAggregatesAsync(Guid entityId, DateTime monthStartUtc)
    {
        var baseQuery = paymentHistory.Query()
            .AsNoTracking()
            .Where(p => p.EntityId == entityId);

        var grossCollectedMinor = await baseQuery
            .Where(x => x.AmountPaid > 0)
            .SumAsync(x => (long?)x.AmountPaid) ?? 0;

        var refundedMinor = await baseQuery
            .Where(x => x.AmountPaid < 0)
            .SumAsync(x => (long?)x.AmountPaid) ?? 0;

        var monthCollectedMinor = await baseQuery
            .Where(x => x.AmountPaid > 0 && x.PaidAt >= monthStartUtc)
            .SumAsync(x => (long?)x.AmountPaid) ?? 0;

        var disputeCount = await baseQuery
            .CountAsync(x => EF.Functions.Like(x.EventType, "%dispute%"));

        return Result<PaymentHistoryAggregateDto>.Success(new PaymentHistoryAggregateDto
        {
            GrossCollectedMinor = grossCollectedMinor,
            RefundedMinorAbsolute = Math.Abs(refundedMinor),
            MonthCollectedMinor = monthCollectedMinor,
            DisputeCount = disputeCount
        });
    }

    private static string BuildCursor(DateTime paidAt, Guid id)
    {
        var raw = $"{paidAt.ToUniversalTime().Ticks}|{id:D}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static bool TryReadCursor(string? cursor, out DateTime paidAt, out Guid id)
    {
        paidAt = default;
        id = Guid.Empty;

        if (string.IsNullOrWhiteSpace(cursor))
            return false;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var split = decoded.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2)
                return false;

            if (!long.TryParse(split[0], out var ticks))
                return false;

            if (!Guid.TryParse(split[1], out id))
                return false;

            paidAt = new DateTime(ticks, DateTimeKind.Utc);
            return true;
        }
        catch
        {
            return false;
        }
    }
}