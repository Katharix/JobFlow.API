using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services
{
    [ScopedService]
    public class SubscriptionRecordService : ISubscriptionRecordService
    {
        private readonly ILogger<SubscriptionRecordService> logger;
        private readonly IUnitOfWork unitOfWork;
        private readonly IRepository<SubscriptionRecord> subscriptions;
        private readonly IRepository<CustomerPaymentProfile> paymentProfiles;

        public SubscriptionRecordService(ILogger<SubscriptionRecordService> logger, IUnitOfWork unitOfWork)
        {
            this.logger = logger;
            this.unitOfWork = unitOfWork;
            this.subscriptions = unitOfWork.RepositoryOf<SubscriptionRecord>();
            this.paymentProfiles = unitOfWork.RepositoryOf<CustomerPaymentProfile>();
        }

        public async Task<Result<SubscriptionRecord>> CreateAsync(Guid paymentProfileId, string providerSubscriptionId, string providerPriceId, string status)
        {
            if (paymentProfileId == Guid.Empty)
                return Result.Failure<SubscriptionRecord>(SubscriptionErrors.InvalidPaymentProfile);

            if (string.IsNullOrWhiteSpace(providerSubscriptionId))
                return Result.Failure<SubscriptionRecord>(SubscriptionErrors.MissingProviderSubscriptionId);
            try
            {
                var paymentProfile = await paymentProfiles.Query().FirstOrDefaultAsync(p => p.Id == paymentProfileId);
                if (paymentProfile == null)
                    return Result.Failure<SubscriptionRecord>(SubscriptionErrors.InvalidPaymentProfile);

                var subscription = new SubscriptionRecord
                {
                    Id = Guid.NewGuid(),
                    PaymentProfileId = paymentProfileId,
                    Provider = paymentProfile.Provider,
                    ProviderSubscriptionId = providerSubscriptionId,
                    ProviderPriceId = providerPriceId,
                    Status = status,
                    StartDate = DateTime.UtcNow
                };

                unitOfWork.RepositoryOf<SubscriptionRecord>().Add(subscription);
                await unitOfWork.SaveChangesAsync();

                return Result.Success(subscription);
            }
            catch (Exception)
            {

                throw;
            }

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
    }
}
