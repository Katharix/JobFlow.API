namespace JobFlow.Business.ModelErrors;

public static class PaymentProfileErrors
{
    public static Error NotFound => Error.NotFound("PaymentProfile", "Payment profile not found.");
    public static Error NullOrEmptyOwnerId => Error.Failure("PaymentProfile", "OwnerId cannot be null or empty.");
    public static Error ProviderCustomerIdMissing => Error.Failure("PaymentProfile", "ProviderCustomerId is required.");
}