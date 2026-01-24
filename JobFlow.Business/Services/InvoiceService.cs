using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Onboarding;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class InvoiceService : IInvoiceService
{
    private readonly IRepository<Invoice> invoices;
    private readonly ILogger<InvoiceService> logger;
    private readonly IOnboardingService _onboardingService;
    private readonly IUnitOfWork unitOfWork;

    public InvoiceService(ILogger<InvoiceService> logger, IUnitOfWork unitOfWork, IOnboardingService onboardingService)
    {
        this.logger = logger;
        this.unitOfWork = unitOfWork;
        invoices = unitOfWork.RepositoryOf<Invoice>();
        _onboardingService = onboardingService;
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

    public async Task<Result<Invoice>> UpsertInvoiceAsync(Invoice model)
    {
        var exists = await invoices.Query().AnyAsync(i => i.Id == model.Id);

        // Calculate TotalAmount manually since it's not mapped
        model.TotalAmount = model.LineItems?.Sum(li => li.Quantity * li.UnitPrice) ?? 0;

        if (exists)
            invoices.Update(model);
        else
        {
            // Ensure invoice ID is set before adding line items (if needed)
            if (model.Id == Guid.Empty)
                model.Id = Guid.NewGuid();

            // Attach invoice to line items
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

        await _onboardingService.MarkStepCompleteAsync(
            invoice.OrganizationId,
            OnboardingStepKeys.SendInvoice
        );
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
        invoice.AmountPaid = amountReceived;
        invoice.PaidAt = DateTimeOffset.UtcNow;
        invoice.PaymentProvider = provider;
        invoice.ExternalPaymentId = externalPaymentId;

        await unitOfWork.SaveChangesAsync();
        return Result.Success(invoice);
    }

}