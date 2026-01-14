using JobFlow.Business.PaymentGateways.SharedModels;

namespace JobFlow.Business.PaymentGateways;

public interface IPaymentProcessor
{
    Task<PaymentSessionResult> CreatePaymentIntentAsync(PaymentSessionRequest request);
    Task<string> CreateSubscriptionCheckoutSessionAsync(PaymentSessionRequest request);
}