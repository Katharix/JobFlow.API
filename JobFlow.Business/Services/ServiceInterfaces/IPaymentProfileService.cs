using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IPaymentProfileService
{
    Task<Result<CustomerPaymentProfile>> GetForOrganizationAsync(Guid organizationId);
    Task<Result<CustomerPaymentProfile>> GetForClientAsync(Guid clientId);

    Task<Result<CustomerPaymentProfile>> CreateAsync(Guid ownerId, PaymentEntityType ownerType,
        PaymentProvider provider, string providerCustomerId);

    Task<Result> SetDefaultPaymentMethodAsync(Guid profileId, string paymentMethodId);
}