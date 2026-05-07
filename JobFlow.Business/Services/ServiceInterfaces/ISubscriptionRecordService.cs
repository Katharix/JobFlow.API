using JobFlow.Domain.Models;
using JobFlow.Domain.Enums;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface ISubscriptionRecordService
{
    Task<Result<SubscriptionRecord>> CreateAsync(Guid paymentProfileId, string providerSubscriptionId,
        string providerPriceId, string status, string planName);

    Task<Result<SubscriptionRecord>> GetByProviderIdAsync(string providerSubscriptionId);
    Task<Result<SubscriptionRecord>> GetLatestForOrganizationAsync(Guid organizationId, PaymentProvider? provider = null);
    Task<Result> CancelAsync(string providerSubscriptionId, DateTime canceledAt);
    Task<Result> UpdateAsync(SubscriptionRecord subscriptionRecord);
}