using JobFlow.Business.PaymentGateways.SharedModels;

namespace JobFlow.Business.PaymentGateways;

public interface IPaymentProcessor
{
    Task<string> CreateCheckoutSessionAsync(PaymentSessionRequest request);
    Task<string> CreateSubscriptionCheckoutSessionAsync(PaymentSessionRequest request);
}