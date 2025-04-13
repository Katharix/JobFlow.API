using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.PaymentGateways
{
    public interface IPaymentProfileService
    {
        Task<Result<CustomerPaymentProfile>> GetForOrganizationAsync(Guid organizationId);
        Task<Result<CustomerPaymentProfile>> GetForClientAsync(Guid clientId);
        Task<Result<CustomerPaymentProfile>> CreateAsync(Guid ownerId, PaymentEntityType ownerType, PaymentProvider provider, string providerCustomerId);
        Task<Result> SetDefaultPaymentMethodAsync(Guid profileId, string paymentMethodId);
    }
}
