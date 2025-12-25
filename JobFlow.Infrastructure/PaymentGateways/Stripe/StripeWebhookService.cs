using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using JobFlow.Infrastructure.PaymentGateways.Stripe.StripeModels;
using Stripe;
using Stripe.Checkout;

namespace JobFlow.Infrastructure.PaymentGateways.Stripe;

[ScopedService]
public class StripeWebhookService : IStripeWebhookService
{
    private readonly IPaymentProfileService _paymentProfileService;
    private readonly ISubscriptionRecordService _subscriptionRecordService;

    public StripeWebhookService(
        IPaymentProfileService paymentProfileService,
        ISubscriptionRecordService subscriptionRecordService)
    {
        _paymentProfileService = paymentProfileService;
        _subscriptionRecordService = subscriptionRecordService;
    }

    public async Task HandleEventAsync(Event stripeEvent)
    {
        switch (stripeEvent.Type)
        {
            case StripeEvents.CheckoutSessionCompleted:
            {
                var session = stripeEvent.Data.Object as Session;
                await HandleCheckoutSessionAsync(session);
                break;
            }
            case StripeEvents.PaymentIntentSucceeded:
            {
                var intent = stripeEvent.Data.Object as PaymentIntent;
                await HandlePaymentIntentAsync(intent);
                break;
            }
            case StripeEvents.PaymentIntentFailed:
            {
                var intent = stripeEvent.Data.Object as PaymentIntent;
                await HandlePaymentIntentFailedAsync(intent);
                break;
            }
            case StripeEvents.InvoicePaymentSucceeded:
            {
                var invoice = stripeEvent.Data.Object as Invoice;
                await HandleInvoicePaymentSucceededAsync(invoice);
                break;
            }
            case StripeEvents.InvoicePaymentFailed:
            {
                var invoice = stripeEvent.Data.Object as Invoice;
                await HandleInvoicePaymentFailedAsync(invoice);
                break;
            }
            case StripeEvents.InvoiceCreated:
            {
                var invoice = stripeEvent.Data.Object as Invoice;
                await HandleInvoiceCreatedAsync(invoice);
                break;
            }
            case StripeEvents.InvoiceFinalized:
            {
                var invoice = stripeEvent.Data.Object as Invoice;
                await HandleInvoiceFinalizedAsync(invoice);
                break;
            }
            case StripeEvents.InvoiceMarkedUncollectible:
            {
                var invoice = stripeEvent.Data.Object as Invoice;
                await HandleInvoiceMarkedUncollectibleAsync(invoice);
                break;
            }
            case StripeEvents.CustomerSubscriptionUpdated:
            {
                var updated = stripeEvent.Data.Object as Subscription;
                await HandleSubscriptionUpdatedAsync(updated);
                break;
            }
            case StripeEvents.CustomerSubscriptionDeleted:
            {
                var deletedSub = stripeEvent.Data.Object as Subscription;
                await _subscriptionRecordService.CancelAsync(
                    deletedSub.Id,
                    DateTime.UtcNow
                );
                break;
            }
            case StripeEvents.SubscriptionCreated:
            {
                var subscription = stripeEvent.Data.Object as Subscription;
                await HandleSubscriptionCreatedAsync(subscription);
                break;
            }
            case StripeEvents.SubscriptionTrialWillEnd:
            {
                var subscription = stripeEvent.Data.Object as Subscription;
                await HandleSubscriptionTrialWillEndAsync(subscription);
                break;
            }
            case StripeEvents.CustomerCreated:
            {
                var customer = stripeEvent.Data.Object as Customer;
                await HandleCustomerCreatedAsync(customer);
                break;
            }
            case StripeEvents.CustomerUpdated:
            {
                var customer = stripeEvent.Data.Object as Customer;
                await HandleCustomerUpdatedAsync(customer);
                break;
            }
            case StripeEvents.CustomerDeleted:
            {
                var customer = stripeEvent.Data.Object as Customer;
                await HandleCustomerDeletedAsync(customer);
                break;
            }
            case StripeEvents.PaymentMethodAttached:
            {
                var paymentMethod = stripeEvent.Data.Object as PaymentMethod;
                await HandlePaymentMethodAttachedAsync(paymentMethod);
                break;
            }
            case StripeEvents.PaymentMethodDetached:
            {
                var paymentMethod = stripeEvent.Data.Object as PaymentMethod;
                await HandlePaymentMethodDetachedAsync(paymentMethod);
                break;
            }
        }
    }

    private async Task HandleCheckoutSessionAsync(Session session)
    {
        var subscriptionService = new SubscriptionService();
        var subscription = await subscriptionService.GetAsync(session.SubscriptionId);

        var ownerId = subscription.Metadata["ownerId"];
        var ownerType = subscription.Metadata["ownerType"];
        var paymentCustomerId = subscription.Metadata["customerId"];

        var paymentProfileResult = await _paymentProfileService.CreateAsync(
            Guid.Parse(ownerId),
            Enum.Parse<PaymentEntityType>(ownerType),
            PaymentProvider.Stripe,
            paymentCustomerId
        );

        await _subscriptionRecordService.CreateAsync(
            paymentProfileResult.Value.Id,
            subscription.Id,
            subscription.Items.Data.First().Price.Id,
            subscription.Status
        );
    }

    private async Task HandlePaymentIntentAsync(PaymentIntent intent)
    {
        // Mark payment as successful, set default payment method if available
        if (!string.IsNullOrEmpty(intent.CustomerId) && !string.IsNullOrEmpty(intent.PaymentMethodId))
        {
            var profileResult = await _paymentProfileService.GetForOrganizationAsync(Guid.Parse(intent.CustomerId));
            if (profileResult.IsSuccess)
                await _paymentProfileService.SetDefaultPaymentMethodAsync(profileResult.Value.Id,
                    intent.PaymentMethodId);
        }
    }

    private async Task HandlePaymentIntentFailedAsync(PaymentIntent intent)
    {
        // Flag payment profile as delinquent
        if (!string.IsNullOrEmpty(intent.CustomerId))
        {
            var profileResult = await _paymentProfileService.GetForOrganizationAsync(Guid.Parse(intent.CustomerId));
            if (profileResult.IsSuccess)
            {
                profileResult.Value.IsDelinquent = true;
                // Persist changes if needed
            }
        }
    }

    private async Task HandleInvoicePaymentSucceededAsync(Invoice invoice)
    {
        // Mark subscription as active
        if (!string.IsNullOrEmpty(invoice.SubscriptionId))
        {
            var subResult = await _subscriptionRecordService.GetByProviderIdAsync(invoice.SubscriptionId);
            if (subResult.IsSuccess)
            {
                subResult.Value.Status = "active";
                // Persist changes if needed
            }
        }
    }

    private async Task HandleInvoicePaymentFailedAsync(Invoice invoice)
    {
        // Mark subscription as past_due
        if (!string.IsNullOrEmpty(invoice.SubscriptionId))
        {
            var subResult = await _subscriptionRecordService.GetByProviderIdAsync(invoice.SubscriptionId);
            if (subResult.IsSuccess)
            {
                subResult.Value.Status = "past_due";
                // Persist changes if needed
            }
        }
    }

    private async Task HandleInvoiceCreatedAsync(Invoice invoice)
    {
        // Optionally log or notify about invoice creation
    }

    private async Task HandleInvoiceFinalizedAsync(Invoice invoice)
    {
        // Optionally log or notify about invoice finalization
    }

    private async Task HandleInvoiceMarkedUncollectibleAsync(Invoice invoice)
    {
        // Optionally flag invoice as uncollectible
    }

    private async Task HandleSubscriptionUpdatedAsync(Subscription subscription)
    {
        // Update subscription status
        var subResult = await _subscriptionRecordService.GetByProviderIdAsync(subscription.Id);
        if (subResult.IsSuccess)
        {
            subResult.Value.Status = subscription.Status;
            // Persist changes if needed
        }
    }

    private async Task HandleSubscriptionCreatedAsync(Subscription subscription)
    {
        var ownerId = subscription.Metadata["ownerId"];
        var ownerType = subscription.Metadata["ownerType"];
        var paymentCustomerId = subscription.Metadata["customerId"];

        var paymentProfileResult = await _paymentProfileService.CreateAsync(
            Guid.Parse(ownerId),
            Enum.Parse<PaymentEntityType>(ownerType),
            PaymentProvider.Stripe,
            paymentCustomerId
        );

        await _subscriptionRecordService.CreateAsync(
            paymentProfileResult.Value.Id,
            subscription.Id,
            subscription.Items.Data.First().Price.Id,
            subscription.Status
        );
    }

    private async Task HandleSubscriptionTrialWillEndAsync(Subscription subscription)
    {
        // Notify user trial is ending
    }

    private async Task HandleCustomerCreatedAsync(Customer customer)
    {
        // Create payment profile for new customer if needed
    }

    private async Task HandleCustomerUpdatedAsync(Customer customer)
    {
        // Update payment profile for customer if needed
    }

    private async Task HandleCustomerDeletedAsync(Customer customer)
    {
        // Remove or deactivate payment profile for customer if needed
    }

    private async Task HandlePaymentMethodAttachedAsync(PaymentMethod paymentMethod)
    {
        // Set as default payment method if needed
    }

    private async Task HandlePaymentMethodDetachedAsync(PaymentMethod paymentMethod)
    {
        // Remove payment method from profile if needed
    }
}

public interface IStripeWebhookService
{
    Task HandleEventAsync(Event stripeEvent);
}