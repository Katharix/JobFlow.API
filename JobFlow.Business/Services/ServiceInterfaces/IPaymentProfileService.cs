using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IPaymentProfileService
{
    Task<Result<CustomerPaymentProfile>> GetForOrganizationAsync(Guid organizationId);
    Task<Result<CustomerPaymentProfile>> GetForOrganizationAsync(Guid organizationId, PaymentProvider provider);
    Task<Result<CustomerPaymentProfile>> GetForClientAsync(Guid clientId);

    Task<Result<CustomerPaymentProfile>> CreateAsync(Guid ownerId, PaymentEntityType ownerType,
        PaymentProvider provider, string providerCustomerId);

    Task<Result<CustomerPaymentProfile>> UpsertAsync(Guid ownerId, PaymentEntityType ownerType,
        PaymentProvider provider, string providerCustomerId);

    Task<Result<CustomerPaymentProfile>> UpsertWithTokensAsync(
        Guid ownerId, PaymentEntityType ownerType, PaymentProvider provider, string providerCustomerId,
        string encryptedAccessToken, string encryptedRefreshToken, DateTime tokenExpiresAtUtc,
        string? squareLocationId);

    Task<Result> SetDefaultPaymentMethodAsync(Guid profileId, string paymentMethodId);

    Task<Result> UpdateTokensAsync(Guid profileId, string encryptedAccessToken,
        string encryptedRefreshToken, DateTime tokenExpiresAtUtc);

    Task<Result> DisconnectOrganizationProviderAsync(Guid organizationId, PaymentProvider provider);
}