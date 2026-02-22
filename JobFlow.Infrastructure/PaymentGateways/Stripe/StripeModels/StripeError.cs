namespace JobFlow.Infrastructure.PaymentGateways.Stripe.StripeModels;

public class StripeError
{
    public string? Code { get; set; }
    public string? DeclineCode { get; set; }
    public string? Message { get; set; }
    public string? Param { get; set; }
    public StripeErrorEnum Type { get; set; }
}

public enum StripeErrorEnum
{
    ApiError,
    CardError,
    IdempotencyError,
    InvalidRequestError
}