namespace JobFlow.Business.PaymentGateways.SharedModels;

public class PaymentSessionResult
{
    public string? RedirectUrl { get; init; }      // Square, Stripe Checkout (legacy)
    public string? ClientSecret { get; init; }     // Stripe PaymentIntent
    public string ProviderPaymentId { get; init; } = default!;
}