using JobFlow.Business.PaymentGateways.SharedModels;

namespace JobFlow.Business.PaymentGateways;

public interface IPaymentProcessor
{
    Task<PaymentSessionResult> CreatePaymentIntentAsync(PaymentSessionRequest request);
    Task<string> CreateSubscriptionCheckoutSessionAsync(PaymentSessionRequest request);
}

public interface IPaymentOperationsProcessor
{
    Task<PaymentOperationResult> RefundPaymentAsync(PaymentRefundRequest request);
    Task<PaymentOperationResult> AdjustPaymentAsync(PaymentAdjustmentRequest request);
    Task<PaymentSessionResult> CreateDepositPaymentAsync(PaymentSessionRequest request);
}

public interface ISubscriptionOperationsProcessor
{
    Task<PaymentOperationResult> CancelSubscriptionAsync(string providerSubscriptionId);
    Task<PaymentOperationResult> ChangeSubscriptionPlanAsync(string providerSubscriptionId, string providerPriceId);
}