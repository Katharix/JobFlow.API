using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Onboarding;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using JobFlow.Business.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class InvoiceService : IInvoiceService
{
    private readonly IRepository<Invoice> invoices;
    private readonly IRepository<Estimate> estimates;
    private readonly IRepository<OrganizationClient> clients;
    private readonly ILogger<InvoiceService> logger;
    private readonly IOnboardingService _onboardingService;
    private readonly IInvoiceNumberGenerator _numberGenerator;
    private readonly INotificationService _notifications;
    private readonly IOrganizationClientPortalService _clientPortal;
    private readonly IInvoiceRealtimeNotifier? _realtimeNotifier;
    private readonly IOrganizationService _organizationService;
    private readonly IUnitOfWork unitOfWork;

    public InvoiceService(
        ILogger<InvoiceService> logger,
        IUnitOfWork unitOfWork,
        IOrganizationService organizationService,
        IOnboardingService onboardingService,
        IInvoiceNumberGenerator numberGenerator,
        INotificationService notifications,
        IOrganizationClientPortalService clientPortal,
        IInvoiceRealtimeNotifier? realtimeNotifier = null)
    {
        this.logger = logger;
        this.unitOfWork = unitOfWork;
        _organizationService = organizationService;
        invoices = unitOfWork.RepositoryOf<Invoice>();
        estimates = unitOfWork.RepositoryOf<Estimate>();
        clients = unitOfWork.RepositoryOf<OrganizationClient>();
        _onboardingService = onboardingService;
        _numberGenerator = numberGenerator;
        _notifications = notifications;
        _clientPortal = clientPortal;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<Result<Invoice>> GetInvoiceByIdAsync(Guid id)
    {
        var invoice = await invoices.Query()
            .Include(e => e.LineItems)
            .Include(e => e.OrganizationClient)
            .ThenInclude(e => e.Organization)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null)
            return Result.Failure<Invoice>(InvoiceErrors.NotFound);

        return Result<Invoice>.Success(invoice);
    }
    public async Task<bool> IsPaidAsync(Guid invoiceId)
    {
        var invoice = await invoices
            .Query()
            .Where(i => i.Id == invoiceId)
            .Select(i => new { i.Status })
            .FirstOrDefaultAsync();

        return invoice?.Status == InvoiceStatus.Paid;
    }

    public async Task<Result<IEnumerable<Invoice>>> GetInvoicesByClientAsync(Guid clientId)
    {
        var list = await invoices.Query().Where(i => i.OrganizationClientId == clientId).ToListAsync();
        return Result<IEnumerable<Invoice>>.Success(list.AsEnumerable());
    }

    public async Task<Result<IEnumerable<Invoice>>> GetInvoicesByOrganizationAsync(Guid organizationId)
    {
        var list = await invoices.Query()
            .Include(i => i.LineItems)
            .Include(i => i.OrganizationClient)
            .ThenInclude(c => c.Organization)
            .Where(i => i.OrganizationId == organizationId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return Result<IEnumerable<Invoice>>.Success(list.AsEnumerable());
    }

    public async Task<Result<CursorPagedResponseDto<Invoice>>> GetInvoicesByOrganizationPagedAsync(
        Guid organizationId,
        int pageSize,
        string? cursor,
        string? statusFilter,
        string? search,
        string? sortBy,
        string? sortDirection)
    {
        var size = Math.Clamp(pageSize, 1, 100);
        var query = invoices.Query()
            .Include(i => i.LineItems)
            .Include(i => i.OrganizationClient)
            .ThenInclude(c => c.Organization)
            .Where(i => i.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(statusFilter)
            && Enum.TryParse<InvoiceStatus>(statusFilter, true, out var parsedStatus))
        {
            query = query.Where(i => i.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i =>
                EF.Functions.Like(i.InvoiceNumber, $"%{term}%")
                || EF.Functions.Like(i.OrganizationClient.FirstName, $"%{term}%")
                || EF.Functions.Like(i.OrganizationClient.LastName, $"%{term}%")
                || (i.OrganizationClient.EmailAddress != null && EF.Functions.Like(i.OrganizationClient.EmailAddress, $"%{term}%")));
        }

        var desc = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy ?? string.Empty).ToLowerInvariant() switch
        {
            "duedate" => desc ? query.OrderByDescending(i => i.DueDate).ThenByDescending(i => i.Id) : query.OrderBy(i => i.DueDate).ThenBy(i => i.Id),
            "invoicedate" => desc ? query.OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.Id) : query.OrderBy(i => i.InvoiceDate).ThenBy(i => i.Id),
            "totalamount" => desc ? query.OrderByDescending(i => i.TotalAmount).ThenByDescending(i => i.Id) : query.OrderBy(i => i.TotalAmount).ThenBy(i => i.Id),
            "status" => desc ? query.OrderByDescending(i => i.Status).ThenByDescending(i => i.Id) : query.OrderBy(i => i.Status).ThenBy(i => i.Id),
            _ => desc ? query.OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id) : query.OrderBy(i => i.CreatedAt).ThenBy(i => i.Id)
        };

        var totalCount = await query.CountAsync();

        var sortKey = (sortBy ?? string.Empty).ToLowerInvariant();
        var useKeysetCursor = sortKey is "" or "createdat";

        if (useKeysetCursor && CursorToken.TryRead(cursor, out var cursorCreatedAt, out var cursorId))
        {
            query = desc
                ? query.Where(i => i.CreatedAt < cursorCreatedAt || (i.CreatedAt == cursorCreatedAt && i.Id.CompareTo(cursorId) < 0))
                : query.Where(i => i.CreatedAt > cursorCreatedAt || (i.CreatedAt == cursorCreatedAt && i.Id.CompareTo(cursorId) > 0));
        }
        else if (!useKeysetCursor && CursorToken.TryReadOffset(cursor, out var offset))
        {
            query = query.Skip(offset);
        }

        var batch = await query
            .Take(size + 1)
            .ToListAsync();

        var hasMore = batch.Count > size;
        var items = hasMore ? batch.Take(size).ToList() : batch;

        string? nextCursor;
        if (!hasMore || items.Count == 0)
        {
            nextCursor = null;
        }
        else if (useKeysetCursor)
        {
            nextCursor = CursorToken.Build(items[^1].CreatedAt, items[^1].Id);
        }
        else
        {
            var currentOffset = CursorToken.TryReadOffset(cursor, out var prevOff) ? prevOff : 0;
            nextCursor = CursorToken.BuildOffset(currentOffset + items.Count);
        }

        return Result<CursorPagedResponseDto<Invoice>>.Success(new CursorPagedResponseDto<Invoice>
        {
            Items = items,
            NextCursor = nextCursor,
            TotalCount = totalCount
        });
    }

    public async Task<Result<InvoiceAggregateDto>> GetInvoiceAggregatesByOrganizationAsync(Guid organizationId)
    {
        var baseQuery = invoices.Query()
            .AsNoTracking()
            .Where(i => i.OrganizationId == organizationId);

        var statusCounts = await baseQuery
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var totals = await baseQuery
            .Select(i => new { i.TotalAmount, i.AmountPaid, i.Status })
            .ToListAsync();

        var invoiceCount = totals.Count;
        var totalBilled = totals.Sum(i => i.TotalAmount);
        var balanceDue = totals.Sum(i => i.TotalAmount - i.AmountPaid);
        var outstanding = totals
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Refunded)
            .Sum(i => i.TotalAmount - i.AmountPaid);

        int CountFor(InvoiceStatus s) => statusCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

        return Result<InvoiceAggregateDto>.Success(new InvoiceAggregateDto
        {
            InvoiceCount = invoiceCount,
            DraftCount = CountFor(InvoiceStatus.Draft),
            SentCount = CountFor(InvoiceStatus.Sent),
            PaidCount = CountFor(InvoiceStatus.Paid),
            OverdueCount = CountFor(InvoiceStatus.Overdue),
            RefundedCount = CountFor(InvoiceStatus.Refunded),
            TotalBilled = totalBilled,
            BalanceDue = balanceDue,
            Outstanding = outstanding
        });
    }

    public async Task<Result<Invoice>> UpsertInvoiceAsync(Invoice model)
    {
        var exists = await invoices.Query().AnyAsync(i => i.Id == model.Id);

        // Calculate TotalAmount manually since it's not mapped
        model.TotalAmount = model.LineItems?.Sum(li => li.Quantity * li.UnitPrice) ?? 0;

        if (!Enum.IsDefined(typeof(PaymentProvider), model.PaymentProvider)
            || model.PaymentProvider == 0)
        {
            var orgResult = await _organizationService.GetOrganiztionById(model.OrganizationId);
            model.PaymentProvider = orgResult.IsSuccess
                ? (orgResult.Value.PaymentProvider == 0 ? PaymentProvider.Stripe : orgResult.Value.PaymentProvider)
                : PaymentProvider.Stripe;
        }

        if (exists)
            invoices.Update(model);
        else
        {
            // Ensure invoice ID is set before adding line items (if needed)
            if (model.Id == Guid.Empty)
                model.Id = Guid.NewGuid();

            // Attach invoice to line items
            if (model.LineItems is not null)
                foreach (var li in model.LineItems) li.InvoiceId = model.Id;

            await invoices.AddAsync(model);
        }

        await unitOfWork.SaveChangesAsync();

        await _onboardingService.MarkStepCompleteAsync(
            model.OrganizationId,
            OnboardingStepKeys.CreateInvoice
        );

        return Result<Invoice>.Success(model);
    }

    public async Task<Result<Invoice>> UpdateInvoiceAsync(Guid id, Guid organizationId, DateTime? invoiceDate, DateTime dueDate, List<InvoiceLineItem> lineItems)
    {
        var invoice = await invoices.Query()
            .Include(i => i.LineItems)
            .Include(i => i.OrganizationClient)
            .ThenInclude(c => c.Organization)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null || invoice.OrganizationId != organizationId)
            return Result.Failure<Invoice>(InvoiceErrors.NotFound);

        if (invoice.Status == InvoiceStatus.Paid)
            return Result.Failure<Invoice>(InvoiceErrors.NotEditable);

        if (invoiceDate.HasValue)
            invoice.InvoiceDate = invoiceDate.Value;

        invoice.DueDate = dueDate;

        var existing = invoice.LineItems.ToList();

        // Update existing line items in place
        for (var i = 0; i < Math.Min(existing.Count, lineItems.Count); i++)
        {
            existing[i].PriceBookItemId = lineItems[i].PriceBookItemId;
            existing[i].Description = lineItems[i].Description;
            existing[i].Quantity = lineItems[i].Quantity;
            existing[i].UnitPrice = lineItems[i].UnitPrice;
        }

        // Soft-delete extras directly on tracked entities (avoid collection Remove which triggers EF cascade delete)
        for (var i = lineItems.Count; i < existing.Count; i++)
        {
            existing[i].IsActive = false;
            existing[i].DeactivatedAtUtc = DateTime.UtcNow;
        }

        // Add new ones if more line items now
        for (var i = existing.Count; i < lineItems.Count; i++)
        {
            invoice.LineItems.Add(new InvoiceLineItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = id,
                PriceBookItemId = lineItems[i].PriceBookItemId,
                Description = lineItems[i].Description,
                Quantity = lineItems[i].Quantity,
                UnitPrice = lineItems[i].UnitPrice
            });
        }

        invoice.TotalAmount = invoice.LineItems.Where(li => li.IsActive).Sum(li => li.Quantity * li.UnitPrice);

        await unitOfWork.SaveChangesAsync();

        return Result<Invoice>.Success(invoice);
    }


    public async Task<Result> DeleteInvoiceAsync(Guid id)
    {
        var entity = await invoices.Query().FirstOrDefaultAsync(i => i.Id == id);
        if (entity == null)
            return Result.Failure(InvoiceErrors.NotFound);

        invoices.Remove(entity);
        await unitOfWork.SaveChangesAsync();
        return Result.Success();
    }


    public async Task MarkInvoiceSentAsync(Guid invoiceId)
    {
        var invoice = await invoices
            .Query()
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
            return;

        if (invoice.Status != InvoiceStatus.Paid)
        {
            invoice.Status = InvoiceStatus.Sent;
            invoices.Update(invoice);
            await unitOfWork.SaveChangesAsync();
        }

        await _onboardingService.MarkStepCompleteAsync(
            invoice.OrganizationId,
            OnboardingStepKeys.SendInvoice
        );
    }

    public async Task<Result> SendInvoiceToClientAsync(Guid invoiceId)
    {
        var invoiceResult = await GetInvoiceByIdAsync(invoiceId);
        if (!invoiceResult.IsSuccess)
            return Result.Failure(invoiceResult.Error);

        await SendInvoiceToClientAsync(invoiceResult.Value);
        return Result.Success();
    }

    public async Task<Result> SendInvoiceForJobAsync(Guid organizationId, Job job)
    {
        var invoice = await invoices.Query()
            .Include(i => i.OrganizationClient)
            .ThenInclude(c => c.Organization)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.OrganizationId == organizationId && i.JobId == job.Id);

        if (invoice == null)
        {
            var createResult = await CreateInvoiceFromEstimateAsync(organizationId, job);
            if (createResult.IsFailure)
                return Result.Failure(createResult.Error);

            invoice = createResult.Value;
        }

        if (invoice.Status == InvoiceStatus.Paid)
            return Result.Success();

        await SendInvoiceToClientAsync(invoice);
        return Result.Success();
    }
    public async Task<Result<Invoice>> MarkPaidAsync(
        Guid invoiceId,
        PaymentProvider provider,
        string externalPaymentId,
        decimal amountReceived)
    {
        var invoice = await invoices.Query()
            .Include(e => e.OrganizationClient)
            .ThenInclude(e => e.Organization)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice == null)
            return Result.Failure<Invoice>(InvoiceErrors.NotFound);

        // 🔒 Idempotency guard
        if (invoice.Status == InvoiceStatus.Paid)
            return Result.Success(invoice);

        invoice.Status = InvoiceStatus.Paid;
        invoice.AmountPaid += amountReceived;
        invoice.PaidAt = DateTimeOffset.UtcNow;
        invoice.PaymentProvider = provider;
        invoice.ExternalPaymentId = externalPaymentId;

        await unitOfWork.SaveChangesAsync();

        if (_realtimeNotifier != null)
        {
            await _realtimeNotifier.NotifyInvoicePaidAsync(invoice);
        }

        return Result.Success(invoice);
    }

    public async Task<Result<Invoice>> RecordDepositAsync(
        Guid invoiceId,
        decimal depositAmount,
        PaymentProvider provider,
        string externalPaymentId)
    {
        var invoice = await invoices.Query()
            .Include(e => e.OrganizationClient)
            .ThenInclude(e => e.Organization)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
            return Result.Failure<Invoice>(InvoiceErrors.NotFound);

        if (invoice.Status == InvoiceStatus.Paid)
            return Result.Success(invoice);

        invoice.AmountPaid += depositAmount;
        invoice.PaymentProvider = provider;
        invoice.ExternalPaymentId = externalPaymentId;

        if (invoice.AmountPaid >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTimeOffset.UtcNow;
        }

        invoices.Update(invoice);
        await unitOfWork.SaveChangesAsync();

        if (invoice.Status == InvoiceStatus.Paid && _realtimeNotifier != null)
        {
            await _realtimeNotifier.NotifyInvoicePaidAsync(invoice);
        }

        return Result.Success(invoice);
    }

    public async Task<Result<Invoice>> RecordRefundAsync(
        Guid invoiceId,
        decimal refundAmount)
    {
        var invoice = await invoices.Query()
            .Include(e => e.OrganizationClient)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
            return Result.Failure<Invoice>(InvoiceErrors.NotFound);

        if (refundAmount <= 0)
            return Result.Failure<Invoice>(InvoiceErrors.InvalidAmount);

        if (invoice.AmountRefunded + refundAmount > invoice.TotalAmount)
            return Result.Failure<Invoice>(InvoiceErrors.RefundExceedsTotal);

        invoice.AmountRefunded += refundAmount;
        invoice.AmountPaid = Math.Max(invoice.AmountPaid - refundAmount, 0);

        if (invoice.AmountRefunded >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Refunded;
        }
        else if (invoice.AmountPaid < invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Sent;
        }

        invoices.Update(invoice);
        await unitOfWork.SaveChangesAsync();

        return Result.Success(invoice);
    }

    private async Task SendInvoiceToClientAsync(Invoice invoice)
    {
        var client = invoice.OrganizationClient;
        if (client == null)
            return;

        string? linkOverride = null;
        var email = client.EmailAddress;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var returnUrl = $"/client-hub/invoices/{invoice.Id}";
            var linkResult = await _clientPortal.CreateMagicLinkAsync(
                invoice.OrganizationId,
                invoice.OrganizationClientId,
                email,
                returnUrl);

            if (linkResult.IsSuccess)
            {
                linkOverride = linkResult.Value;
            }
        }

        await _notifications.SendClientInvoiceCreatedNotificationAsync(client, invoice, linkOverride);
        await MarkInvoiceSentAsync(invoice.Id);
    }

    private async Task<Result<Invoice>> CreateInvoiceFromEstimateAsync(Guid organizationId, Job job)
    {
        Estimate? estimate = null;

        // Prefer direct link via EstimateId on Job
        if (job.EstimateId.HasValue)
        {
            estimate = await estimates.Query()
                .Include(e => e.LineItems)
                .FirstOrDefaultAsync(e =>
                    e.Id == job.EstimateId.Value &&
                    e.Status == EstimateStatus.Accepted);
        }

        // Fallback: match by client + accepted status
        estimate ??= await estimates.Query()
            .Include(e => e.LineItems)
            .OrderByDescending(e => e.UpdatedAt ?? e.CreatedAt)
            .FirstOrDefaultAsync(e =>
                e.OrganizationId == organizationId &&
                e.OrganizationClientId == job.OrganizationClientId &&
                e.Status == EstimateStatus.Accepted);

        if (estimate == null)
            return Result.Failure<Invoice>(EstimateErrors.NotFound);

        var client = await clients.Query()
            .Include(c => c.Organization)
            .FirstOrDefaultAsync(c => c.Id == job.OrganizationClientId);

        if (client == null)
            return Result.Failure<Invoice>(EstimateErrors.ClientNotFound);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            OrganizationClientId = job.OrganizationClientId,
            JobId = job.Id,
            EstimateId = estimate.Id,
            InvoiceNumber = await _numberGenerator.GenerateAsync(organizationId),
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(14),
            Status = InvoiceStatus.Draft,
            OrganizationClient = client,
            LineItems = estimate.LineItems.Select(li => new InvoiceLineItem
            {
                Id = Guid.NewGuid(),
                PriceBookItemId = li.PriceBookItemId,
                Description = string.IsNullOrWhiteSpace(li.Description) ? li.Name : li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice
            }).ToList()
        };

        var result = await UpsertInvoiceAsync(invoice);
        if (!result.IsSuccess)
            return Result.Failure<Invoice>(result.Error);

        var hydrated = await GetInvoiceByIdAsync(result.Value.Id);
        return hydrated.IsSuccess
            ? hydrated
            : Result.Failure<Invoice>(hydrated.Error);
    }

}