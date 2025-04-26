
using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services
{
    [ScopedService]
    public class PaymentProfileService : IPaymentProfileService
    {
        private readonly ILogger<PaymentProfileService> logger;
        private readonly IUnitOfWork unitOfWork;
        private readonly IRepository<CustomerPaymentProfile> paymentProfiles;

        public PaymentProfileService(ILogger<PaymentProfileService> logger, IUnitOfWork unitOfWork)
        {
            this.logger = logger;
            this.unitOfWork = unitOfWork;
            this.paymentProfiles = unitOfWork.RepositoryOf<CustomerPaymentProfile>();
        }

        public async Task<Result<CustomerPaymentProfile>> GetForOrganizationAsync(Guid organizationId)
        {
            if (organizationId == Guid.Empty)
                return Result.Failure<CustomerPaymentProfile>(OrganizationErrors.NullOrEmptyId);

            var profile = await paymentProfiles.Query()
                .FirstOrDefaultAsync(p => p.OwnerId == organizationId && p.OwnerType == PaymentEntityType.Organization);

            return profile is null
                ? Result.Failure<CustomerPaymentProfile>(PaymentProfileErrors.NotFound)
                : Result.Success(profile);
        }

        public async Task<Result<CustomerPaymentProfile>> GetForClientAsync(Guid clientId)
        {
            if (clientId == Guid.Empty)
                return Result.Failure<CustomerPaymentProfile>(OrganizationErrors.NullOrEmptyId);

            var profile = await paymentProfiles.Query()
                .FirstOrDefaultAsync(p => p.OwnerId == clientId && p.OwnerType == PaymentEntityType.Client);

            return profile is null
                ? Result.Failure<CustomerPaymentProfile>(PaymentProfileErrors.NotFound)
                : Result.Success(profile);
        }

        public async Task<Result<CustomerPaymentProfile>> CreateAsync(Guid ownerId, PaymentEntityType ownerType, PaymentProvider provider, string providerCustomerId)
        {
            if (ownerId == Guid.Empty)
                return Result.Failure<CustomerPaymentProfile>(PaymentProfileErrors.NullOrEmptyOwnerId);

            if (string.IsNullOrWhiteSpace(providerCustomerId))
                return Result.Failure<CustomerPaymentProfile>(PaymentProfileErrors.ProviderCustomerIdMissing);

            var profile = new CustomerPaymentProfile
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                OwnerType = ownerType,
                Provider = provider,
                ProviderCustomerId = providerCustomerId,
                CreatedAt = DateTime.UtcNow
            };

            paymentProfiles.Add(profile);
            await unitOfWork.SaveChangesAsync();

            return Result.Success(profile);
        }

        public async Task<Result> SetDefaultPaymentMethodAsync(Guid profileId, string paymentMethodId)
        {
            var profile = await paymentProfiles.Query().FirstOrDefaultAsync(p => p.Id == profileId);
            if (profile == null)
                return Result.Failure(PaymentProfileErrors.NotFound);

            profile.DefaultPaymentMethodId = paymentMethodId;
            await unitOfWork.SaveChangesAsync();

            return Result.Success();
        }
    }
}


