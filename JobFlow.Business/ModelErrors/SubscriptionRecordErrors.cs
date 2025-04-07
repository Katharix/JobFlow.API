namespace JobFlow.Business.ModelErrors
{
    public static class SubscriptionErrors
    {
        public static Error NotFound => Error.NotFound("Subscription", "Subscription not found.");
        public static Error InvalidPaymentProfile => Error.Failure("Subscription", "Invalid or missing payment profile.");
        public static Error MissingProviderSubscriptionId => Error.Failure("Subscription", "Provider subscription ID is required.");
    }
}
