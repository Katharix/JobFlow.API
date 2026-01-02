namespace JobFlow.Infrastructure.PaymentGateways.Stripe.StripeModels;

public static class StripeEvents
{
    public const string CheckoutSessionCompleted = "checkout.session.completed";
    public const string InvoicePaymentSucceeded = "invoice.payment_succeeded";
    public const string InvoicePaymentFailed = "invoice.payment_failed";
    public const string CustomerSubscriptionUpdated = "customer.subscription.updated";
    public const string CustomerSubscriptionDeleted = "customer.subscription.deleted";
    public const string PaymentIntentSucceeded = "payment_intent.succeeded";
    public const string PaymentIntentFailed = "payment_intent.payment_failed";

    public const string InvoiceCreated = "invoice.created";
    public const string InvoiceFinalized = "invoice.finalized";
    public const string InvoiceMarkedUncollectible = "invoice.marked_uncollectible";
    public const string SubscriptionCreated = "customer.subscription.created";
    public const string SubscriptionTrialWillEnd = "customer.subscription.trial_will_end";
    public const string CustomerCreated = "customer.created";
    public const string CustomerUpdated = "customer.updated";
    public const string CustomerDeleted = "customer.deleted";
    public const string PaymentMethodAttached = "payment_method.attached";
    public const string PaymentMethodDetached = "payment_method.detached";
    public const string AccountUpdated = "account.updated";
    public const string AccountDeleted = "account.deleted";
}