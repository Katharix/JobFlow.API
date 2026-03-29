using JobFlow.Domain.Enums;

namespace JobFlow.Business.PaymentGateways;

public interface IPaymentProcessorFactory
{
    IPaymentProcessor GetProcessor(string provider);
    IPaymentProcessor GetProcessor(PaymentProvider provider);
    Task<IPaymentProcessor> GetProcessorForOrgAsync(Guid organizationId, PaymentProvider provider);
}