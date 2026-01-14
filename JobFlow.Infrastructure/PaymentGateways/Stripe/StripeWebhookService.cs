using JobFlow.Business.DI;
using JobFlow.Business.Extensions;
using JobFlow.Business.Onboarding;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using JobFlow.Infrastructure.PaymentGateways.Stripe.StripeModels;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace JobFlow.Infrastructure.PaymentGateways.Stripe;

[ScopedService]
public class StripeWebhookService : IStripeWebhookService
{
    private readonly IPaymentProfileService _paymentProfileService;
    private readonly ISubscriptionRecordService _subscriptionRecordService;
    private readonly IOrganizationService _organizationService;
    private readonly IInvoiceService _invoiceService;
    private readonly IOnboardingService _onboardingService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<StripeWebhookService> _logger;

    public StripeWebhookService(
        IPaymentProfileService paymentProfileService,
        ISubscriptionRecordService subscriptionRecordService,
        IOrganizationService organizationService,
        IInvoiceService invoiceService,
        IOnboardingService onboardingService,
        INotificationService notificationService,
        ILogger<StripeWebhookService> logger)
    {
        _paymentProfileService = paymentProfileService;
        _subscriptionRecordService = subscriptionRecordService;
        _organizationService = organizationService;
        _invoiceService = invoiceService;
        _onboardingService = onboardingService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleEventAsync(Event stripeEvent)
{
    _logger.LogInformation(
        "Stripe webhook received: EventId={EventId}, Type={Type}, ObjectType={ObjectType}",
        stripeEvent.Id,
        stripeEvent.Type,
        stripeEvent.Data.Object?.GetType().Name
    );

    switch (stripeEvent.Type)
    {
        case StripeEvents.CheckoutSessionCompleted:
        {
            var session = Deserialize<Session>(stripeEvent);
            if (session != null)
                await HandleCheckoutSessionAsync(session);
            break;
        }

        case StripeEvents.AccountUpdated:
        {
            var account = Deserialize<Account>(stripeEvent);
            if (account?.ChargesEnabled == true && account.PayoutsEnabled == true)
                await _organizationService.MarkStripeConnectedAsync(account.Id);
            break;
        }

        case StripeEvents.PaymentIntentSucceeded:
        {
            var intent = Deserialize<PaymentIntent>(stripeEvent);

            if (intent == null)
                throw new InvalidOperationException(
                    $"Stripe webhook deserialization failed. EventId={stripeEvent.Id}, Type={stripeEvent.Type}"
                );

            await HandlePaymentIntentAsync(intent);
            break;
        }


        case StripeEvents.PaymentIntentFailed:
        {
            var intent = Deserialize<PaymentIntent>(stripeEvent);
            if (intent != null)
                await HandlePaymentIntentFailedAsync(intent);
            break;
        }

        case StripeEvents.InvoiceCreated:
        {
            var invoice = Deserialize<Invoice>(stripeEvent);
            if (invoice != null)
                await HandleInvoiceCreatedAsync(invoice);
            break;
        }

        case StripeEvents.InvoiceFinalized:
        {
            var invoice = Deserialize<Invoice>(stripeEvent);
            if (invoice != null)
                await HandleInvoiceFinalizedAsync(invoice);
            break;
        }

        case StripeEvents.InvoiceMarkedUncollectible:
        {
            var invoice = Deserialize<Invoice>(stripeEvent);
            if (invoice != null)
                await HandleInvoiceMarkedUncollectibleAsync(invoice);
            break;
        }

        case StripeEvents.CustomerSubscriptionUpdated:
        {
            var subscription = Deserialize<Subscription>(stripeEvent);
            if (subscription != null)
                await HandleSubscriptionUpdatedAsync(subscription);
            break;
        }

        case StripeEvents.CustomerSubscriptionDeleted:
        {
            var subscription = Deserialize<Subscription>(stripeEvent);
            if (subscription != null)
                await _subscriptionRecordService.CancelAsync(
                    subscription.Id,
                    DateTime.UtcNow
                );
            break;
        }

        case StripeEvents.SubscriptionCreated:
        {
            var subscription = Deserialize<Subscription>(stripeEvent);
            if (subscription != null)
                await HandleSubscriptionCreatedAsync(subscription);
            break;
        }

        case StripeEvents.SubscriptionTrialWillEnd:
        {
            var subscription = Deserialize<Subscription>(stripeEvent);
            if (subscription != null)
                await HandleSubscriptionTrialWillEndAsync(subscription);
            break;
        }

        case StripeEvents.CustomerCreated:
        {
            var customer = Deserialize<Customer>(stripeEvent);
            if (customer != null)
                await HandleCustomerCreatedAsync(customer);
            break;
        }

        case StripeEvents.CustomerUpdated:
        {
            var customer = Deserialize<Customer>(stripeEvent);
            if (customer != null)
                await HandleCustomerUpdatedAsync(customer);
            break;
        }

        case StripeEvents.CustomerDeleted:
        {
            var customer = Deserialize<Customer>(stripeEvent);
            if (customer != null)
                await HandleCustomerDeletedAsync(customer);
            break;
        }

        case StripeEvents.PaymentMethodAttached:
        {
            var method = Deserialize<PaymentMethod>(stripeEvent);
            if (method != null)
                await HandlePaymentMethodAttachedAsync(method);
            break;
        }

        case StripeEvents.PaymentMethodDetached:
        {
            var method = Deserialize<PaymentMethod>(stripeEvent);
            if (method != null)
                await HandlePaymentMethodDetachedAsync(method);
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
        if (intent.Status != "succeeded")
            return;

        if (!intent.Metadata.TryGetValue("invoiceId", out var invoiceIdRaw))
            return;

        if (!Guid.TryParse(invoiceIdRaw, out var invoiceId))
            return;

        var result = await _invoiceService.MarkPaidAsync(
            invoiceId,
            PaymentProvider.Stripe,
            intent.Id,
            intent.AmountReceived / 100m
        );

        if (!result.IsSuccess)
            return;

        var invoice = result.Value;

        // await _notificationService.SendClientPaymentReceivedNotificationAsync(
        //     invoice.OrganizationClient,
        //     invoice
        // );

        await _onboardingService.MarkStepCompleteAsync(
            invoice.OrganizationClient.OrganizationId,
            OnboardingStepKeys.ReceivePayment
        );
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
    
    private static T? Deserialize<T>(Event stripeEvent) where T : StripeEntity
    {
        return stripeEvent.Data.Object as T;
    }
}

public interface IStripeWebhookService
{
    Task HandleEventAsync(Event stripeEvent);
}