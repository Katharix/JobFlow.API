using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class PaymentProfileService : IPaymentProfileService
{
    private readonly ILogger<PaymentProfileService> logger;
    private readonly IRepository<CustomerPaymentProfile> paymentProfiles;
    private readonly IUnitOfWork unitOfWork;

    public PaymentProfileService(ILogger<PaymentProfileService> logger, IUnitOfWork unitOfWork)
    {
        this.logger = logger;
        this.unitOfWork = unitOfWork;
        paymentProfiles = unitOfWork.RepositoryOf<CustomerPaymentProfile>();
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

    public async Task<Result<CustomerPaymentProfile>> GetForOrganizationAsync(Guid organizationId, PaymentProvider provider)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure<CustomerPaymentProfile>(OrganizationErrors.NullOrEmptyId);

        var profile = await paymentProfiles.Query()
            .FirstOrDefaultAsync(p => p.OwnerId == organizationId
                                      && p.OwnerType == PaymentEntityType.Organization
                                      && p.Provider == provider);

        return profile is null
            ? Result.Failure<CustomerPaymentProfile>(PaymentProfileErrors.NotFound)
            : Result.Success(profile);
    }

    public async Task<Result<CustomerPaymentProfile>> GetForClientAsync(Guid clientId)
    {
        if (clientId == Guid.Empty)
            return Result.Failure<CustomerPaymentProfile>(OrganizationErrors.NullOrEmptyId);

        var profile = await paymentProfiles.Query()
            .FirstOrDefaultAsync(p => p.OwnerId == clientId && p.OwnerType == PaymentEntityType.Customer);

        return profile is null
            ? Result.Failure<CustomerPaymentProfile>(PaymentProfileErrors.NotFound)
            : Result.Success(profile);
    }

    public async Task<Result<CustomerPaymentProfile>> CreateAsync(Guid ownerId, PaymentEntityType ownerType,
        PaymentProvider provider, string providerCustomerId)
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

    public async Task<Result<CustomerPaymentProfile>> UpsertAsync(Guid ownerId, PaymentEntityType ownerType,
        PaymentProvider provider, string providerCustomerId)
    {
        if (ownerId == Guid.Empty)
            return Result.Failure<CustomerPaymentProfile>(PaymentProfileErrors.NullOrEmptyOwnerId);

        if (string.IsNullOrWhiteSpace(providerCustomerId))
            return Result.Failure<CustomerPaymentProfile>(PaymentProfileErrors.ProviderCustomerIdMissing);

        var existing = await paymentProfiles.Query()
            .FirstOrDefaultAsync(p => p.OwnerId == ownerId && p.OwnerType == ownerType && p.Provider == provider);

        if (existing != null)
        {
            existing.ProviderCustomerId = providerCustomerId;
            existing.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync();
            return Result.Success(existing);
        }

        return await CreateAsync(ownerId, ownerType, provider, providerCustomerId);
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

    public async Task<Result<CustomerPaymentProfile>> UpsertWithTokensAsync(
        Guid ownerId, PaymentEntityType ownerType, PaymentProvider provider, string providerCustomerId,
        string encryptedAccessToken, string encryptedRefreshToken, DateTime tokenExpiresAtUtc,
        string? squareLocationId)
    {
        if (ownerId == Guid.Empty)
            return Result.Failure<CustomerPaymentProfile>(PaymentProfileErrors.NullOrEmptyOwnerId);

        if (string.IsNullOrWhiteSpace(providerCustomerId))
            return Result.Failure<CustomerPaymentProfile>(PaymentProfileErrors.ProviderCustomerIdMissing);

        var existing = await paymentProfiles.Query()
            .FirstOrDefaultAsync(p => p.OwnerId == ownerId && p.OwnerType == ownerType && p.Provider == provider);

        if (existing != null)
        {
            existing.ProviderCustomerId = providerCustomerId;
            existing.EncryptedAccessToken = encryptedAccessToken;
            existing.EncryptedRefreshToken = encryptedRefreshToken;
            existing.TokenExpiresAtUtc = tokenExpiresAtUtc;
            existing.SquareLocationId = squareLocationId;
            existing.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync();
            return Result.Success(existing);
        }

        var profile = new CustomerPaymentProfile
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            OwnerType = ownerType,
            Provider = provider,
            ProviderCustomerId = providerCustomerId,
            EncryptedAccessToken = encryptedAccessToken,
            EncryptedRefreshToken = encryptedRefreshToken,
            TokenExpiresAtUtc = tokenExpiresAtUtc,
            SquareLocationId = squareLocationId,
            CreatedAt = DateTime.UtcNow
        };

        paymentProfiles.Add(profile);
        await unitOfWork.SaveChangesAsync();

        return Result.Success(profile);
    }

    public async Task<Result> UpdateTokensAsync(Guid profileId, string encryptedAccessToken,
        string encryptedRefreshToken, DateTime tokenExpiresAtUtc)
    {
        var profile = await paymentProfiles.Query().FirstOrDefaultAsync(p => p.Id == profileId);
        if (profile == null)
            return Result.Failure(PaymentProfileErrors.NotFound);

        profile.EncryptedAccessToken = encryptedAccessToken;
        profile.EncryptedRefreshToken = encryptedRefreshToken;
        profile.TokenExpiresAtUtc = tokenExpiresAtUtc;
        profile.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return Result.Success();
    }
}