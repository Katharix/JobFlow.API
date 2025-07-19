using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface ISubscriptionRecordService
    {
        Task<Result<SubscriptionRecord>> CreateAsync(Guid paymentProfileId, string providerSubscriptionId, string providerPriceId, string status);
        Task<Result<SubscriptionRecord>> GetByProviderIdAsync(string providerSubscriptionId);
        Task<Result> CancelAsync(string providerSubscriptionId, DateTime canceledAt);
    }
}
