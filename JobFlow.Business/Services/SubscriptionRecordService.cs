using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class SubscriptionRecordService : ISubscriptionRecordService
{
    private readonly ILogger<SubscriptionRecordService> logger;
    private readonly IRepository<CustomerPaymentProfile> paymentProfiles;
    private readonly IRepository<SubscriptionRecord> subscriptions;
    private readonly IUnitOfWork unitOfWork;

    public SubscriptionRecordService(ILogger<SubscriptionRecordService> logger, IUnitOfWork unitOfWork)
    {
        this.logger = logger;
        this.unitOfWork = unitOfWork;
        subscriptions = unitOfWork.RepositoryOf<SubscriptionRecord>();
        paymentProfiles = unitOfWork.RepositoryOf<CustomerPaymentProfile>();
    }

    public async Task<Result<SubscriptionRecord>> CreateAsync(Guid paymentProfileId, string providerSubscriptionId,
        string providerPriceId, string status, string planName)
    {
        if (paymentProfileId == Guid.Empty)
            return Result.Failure<SubscriptionRecord>(SubscriptionErrors.InvalidPaymentProfile);

        if (string.IsNullOrWhiteSpace(providerSubscriptionId))
            return Result.Failure<SubscriptionRecord>(SubscriptionErrors.MissingProviderSubscriptionId);

        providerSubscriptionId = providerSubscriptionId.Trim();

        var paymentProfile = await paymentProfiles.Query().FirstOrDefaultAsync(p => p.Id == paymentProfileId);
        if (paymentProfile == null)
            return Result.Failure<SubscriptionRecord>(SubscriptionErrors.InvalidPaymentProfile);

        var existing = await subscriptions.Query()
            .FirstOrDefaultAsync(s => s.ProviderSubscriptionId == providerSubscriptionId);

        if (existing is not null)
        {
            if (existing.PaymentProfileId != paymentProfileId)
                return Result.Failure<SubscriptionRecord>(Error.Conflict(
                    "Subscription",
                    "Provider subscription ID is already linked to a different payment profile."));

            return Result.Success(existing);
        }

        var subscription = new SubscriptionRecord
        {
            Id = Guid.NewGuid(),
            PaymentProfileId = paymentProfileId,
            Provider = paymentProfile.Provider,
            ProviderSubscriptionId = providerSubscriptionId,
            ProviderPriceId = providerPriceId,
            Status = status,
            StartDate = DateTime.UtcNow,
            PlanName = planName
        };

        unitOfWork.RepositoryOf<SubscriptionRecord>().Add(subscription);
        await unitOfWork.SaveChangesAsync();

        return Result.Success(subscription);
    }

    public async Task<Result<SubscriptionRecord>> GetByProviderIdAsync(string providerSubscriptionId)
    {
        var subscription = await subscriptions.Query()
            .FirstOrDefaultAsync(s => s.ProviderSubscriptionId == providerSubscriptionId);

        return subscription is null
            ? Result.Failure<SubscriptionRecord>(SubscriptionErrors.NotFound)
            : Result.Success(subscription);
    }

    public async Task<Result> CancelAsync(string providerSubscriptionId, DateTime canceledAt)
    {
        var result = await GetByProviderIdAsync(providerSubscriptionId);
        if (result.IsFailure)
            return result;

        var subscription = result.Value;
        subscription.Status = "canceled";
        subscription.CanceledAt = canceledAt;

        await unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateAsync(SubscriptionRecord subscriptionRecord)
    {
        if (subscriptionRecord is null)
            throw new ArgumentNullException(nameof(subscriptionRecord));

        var existing = await subscriptions.Query()
            .FirstOrDefaultAsync(s => s.Id == subscriptionRecord.Id);

        if (existing is null)
            return Result.Failure(SubscriptionErrors.NotFound);

        existing.ProviderPriceId = subscriptionRecord.ProviderPriceId;
        existing.PlanName = subscriptionRecord.PlanName;
        existing.Status = subscriptionRecord.Status;
        existing.CanceledAt = subscriptionRecord.CanceledAt;
        existing.StartDate = subscriptionRecord.StartDate;

        await unitOfWork.SaveChangesAsync();
        return Result.Success();
    }
}