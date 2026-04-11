using JobFlow.Business.DI;
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
    private readonly IPaymentHistoryService _paymentHistoryService;
    private readonly IOnboardingService _onboardingService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<StripeWebhookService> _logger;

    public StripeWebhookService(
        IPaymentProfileService paymentProfileService,
        ISubscriptionRecordService subscriptionRecordService,
        IOrganizationService organizationService,
        IInvoiceService invoiceService,
        IPaymentHistoryService paymentHistoryService,
        IOnboardingService onboardingService,
        INotificationService notificationService,
        ILogger<StripeWebhookService> logger)
    {
        _paymentProfileService = paymentProfileService;
        _subscriptionRecordService = subscriptionRecordService;
        _organizationService = organizationService;
        _invoiceService = invoiceService;
        _paymentHistoryService = paymentHistoryService;
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
                    if (account == null) return;

                    if (account.ChargesEnabled &&
                        account.PayoutsEnabled &&
                        account.DetailsSubmitted)
                    {
                        await _organizationService.MarkStripeConnectedAsync(account.Id);
                    }
                    else if (!account.ChargesEnabled || !account.PayoutsEnabled)
                    {
                        _logger.LogInformation(
                            "Stripe account {AccountId} is not fully connected. ChargesEnabled={Charges}, PayoutsEnabled={Payouts}",
                            account.Id, account.ChargesEnabled, account.PayoutsEnabled);
                    }

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

            case StripeEvents.InvoicePaymentFailed:
                {
                    var invoice = Deserialize<Invoice>(stripeEvent);
                    if (invoice != null)
                        await HandleInvoicePaymentFailedAsync(invoice, stripeEvent.Type);
                    break;
                }

            case StripeEvents.ChargeRefunded:
                {
                    var charge = Deserialize<Charge>(stripeEvent);
                    if (charge != null)
                        await HandleChargeRefundedAsync(charge, stripeEvent.Type);
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
                    {
                        await _subscriptionRecordService.CancelAsync(
                            subscription.Id,
                            DateTime.UtcNow
                        );

                        if (TryGetSubscriptionOwnerMetadata(subscription, out var ownerId, out var ownerType)
                            && ownerType.Equals(PaymentEntityType.Organization.ToString(), StringComparison.OrdinalIgnoreCase)
                            && Guid.TryParse(ownerId, out var orgId))
                        {
                            await _organizationService.UpdateSubscriptionStateAsync(orgId, "canceled", null, DateTime.UtcNow);
                        }
                    }
                    break;
                }

            case StripeEvents.CustomerSubscriptionPaused:
            case StripeEvents.CustomerSubscriptionResumed:
                {
                    var subscription = Deserialize<Subscription>(stripeEvent);
                    if (subscription != null)
                        await HandleSubscriptionUpdatedAsync(subscription);
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

            case StripeEvents.ChargeDisputeCreated:
            case StripeEvents.ChargeDisputeUpdated:
            case StripeEvents.ChargeDisputeClosed:
                {
                    var dispute = Deserialize<Dispute>(stripeEvent);
                    if (dispute != null)
                        await HandleDisputeAsync(dispute, stripeEvent.Type);
                    break;
                }

            case StripeEvents.PayoutCreated:
            case StripeEvents.PayoutPaid:
            case StripeEvents.PayoutFailed:
                {
                    var payout = Deserialize<Payout>(stripeEvent);
                    if (payout != null)
                        await HandleLedgerEventAsync(
                            stripeEvent.Type,
                            payout.Id,
                            payout.Amount,
                            payout.Currency,
                            payout.Status);
                    break;
                }

            case StripeEvents.TransferCreated:
            case StripeEvents.TransferFailed:
                {
                    var transfer = Deserialize<Transfer>(stripeEvent);
                    if (transfer != null)
                        await HandleLedgerEventAsync(
                            stripeEvent.Type,
                            transfer.Id,
                            transfer.Amount,
                            transfer.Currency,
                            transfer.Reversed ? "reversed" : "created");
                    break;
                }
        }
    }

    private async Task HandleCheckoutSessionAsync(Session session)
    {
        if (string.IsNullOrWhiteSpace(session.SubscriptionId))
        {
            _logger.LogWarning("Stripe checkout.session.completed has no SubscriptionId. SessionId={SessionId}", session.Id);
            return;
        }

        var subscriptionService = new SubscriptionService();
        var subscription = await subscriptionService.GetAsync(session.SubscriptionId);

        if (!subscription.Metadata.TryGetValue("ownerId", out var ownerId) ||
            !subscription.Metadata.TryGetValue("ownerType", out var ownerType) ||
            !subscription.Metadata.TryGetValue("customerId", out var paymentCustomerId))
        {
            _logger.LogWarning(
                "Stripe subscription missing required metadata (ownerId/ownerType/customerId). SubscriptionId={SubscriptionId}",
                subscription.Id);
            return;
        }

        var priceId = subscription.Items?.Data?.FirstOrDefault()?.Price?.Id;
        var planName = subscription.Items?.Data?.FirstOrDefault()?.Price?.Metadata?.GetValueOrDefault("plan-name");

        if (string.IsNullOrWhiteSpace(priceId))
        {
            _logger.LogWarning("Stripe subscription missing price id. SubscriptionId={SubscriptionId}", subscription.Id);
            return;
        }

        if (string.IsNullOrWhiteSpace(planName))
        {
            planName = "Unknown";
        }

        var existingSubscription = await _subscriptionRecordService.GetByProviderIdAsync(subscription.Id);

        var paymentProfileResult = await _paymentProfileService.UpsertAsync(
            Guid.Parse(ownerId),
            Enum.Parse<PaymentEntityType>(ownerType),
            PaymentProvider.Stripe,
            paymentCustomerId
        );

        await _subscriptionRecordService.CreateAsync(
            paymentProfileResult.Value.Id,
            subscription.Id,
            priceId,
            subscription.Status,
            planName
        );

        if (Enum.TryParse<PaymentEntityType>(ownerType, true, out var parsedOwnerType)
            && parsedOwnerType == PaymentEntityType.Organization
            && Guid.TryParse(ownerId, out var organizationId))
        {
            var periodEndUtc = ResolveSubscriptionExpiryUtc(subscription);
            await _organizationService.UpdateSubscriptionStateAsync(organizationId, subscription.Status, planName, periodEndUtc);
            await _paymentProfileService.SetDelinquentByProviderCustomerAsync(
                PaymentProvider.Stripe,
                paymentCustomerId,
                IsDelinquentStatus(subscription.Status));
        }

        if (existingSubscription.IsFailure)
        {
            await TrySendOrganizationWelcomeAsync(ownerId, ownerType, subscription.Status, subscription.Id, StripeEvents.CheckoutSessionCompleted);
        }
    }

    private async Task HandleInvoicePaymentFailedAsync(Invoice invoice, string eventType)
    {
        var customerId = invoice.CustomerId;
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            await _paymentProfileService.SetDelinquentByProviderCustomerAsync(PaymentProvider.Stripe, customerId, true);
        }

        await _paymentHistoryService.LogAsync(new JobFlow.Domain.Models.PaymentHistory
        {
            Id = Guid.NewGuid(),
            PaymentProvider = PaymentProvider.Stripe,
            EntityType = PaymentEntityType.Organization,
            EntityId = Guid.Empty,
            StripeInvoiceId = invoice.Id,
            CustomerId = customerId,
            AmountPaid = invoice.AmountDue,
            Currency = invoice.Currency ?? "usd",
            Status = invoice.Status ?? "payment_failed",
            EventType = eventType,
            PaidAt = DateTime.UtcNow,
            RawEventJson = "{}"
        });
    }

    private async Task HandlePaymentIntentAsync(PaymentIntent intent)
    {
        if (intent.Status != "succeeded")
            return;

        if (!intent.Metadata.TryGetValue("invoiceId", out var invoiceIdRaw))
            return;

        if (!Guid.TryParse(invoiceIdRaw, out var invoiceId))
            return;

        if (await _invoiceService.IsPaidAsync(invoiceId))
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

        await _paymentHistoryService.LogAsync(new JobFlow.Domain.Models.PaymentHistory
        {
            Id = Guid.NewGuid(),
            PaymentProvider = PaymentProvider.Stripe,
            EntityType = PaymentEntityType.Organization,
            EntityId = invoice.OrganizationId,
            InvoiceId = invoice.Id,
            StripePaymentIntentId = intent.Id,
            AmountPaid = intent.AmountReceived,
            Currency = intent.Currency,
            Status = intent.Status,
            EventType = StripeEvents.PaymentIntentSucceeded,
            PaidAt = DateTime.UtcNow,
            RawEventJson = "{}"
        });

        await _onboardingService.MarkStepCompleteAsync(
            invoice.OrganizationClient.OrganizationId,
            OnboardingStepKeys.ReceivePayment
        );

        await _notificationService.SendClientPaymentReceivedNotificationAsync(invoice.OrganizationClient, invoice);
        await _notificationService.SendOrganizationInvoicePaymentReceivedNotificationAsync(
            invoice.OrganizationClient.Organization, invoice.OrganizationClient, invoice, intent.AmountReceived / 100m);
    }


    private async Task HandlePaymentIntentFailedAsync(PaymentIntent intent)
    {
        Guid entityId = Guid.Empty;
        if (intent.Metadata.TryGetValue("invoiceId", out var failedInvoiceId) &&
            Guid.TryParse(failedInvoiceId, out var parsedInvoiceId))
        {
            var invoiceResult = await _invoiceService.GetInvoiceByIdAsync(parsedInvoiceId);
            if (invoiceResult.IsSuccess)
                entityId = invoiceResult.Value.OrganizationId;
        }

        await _paymentHistoryService.LogAsync(new JobFlow.Domain.Models.PaymentHistory
        {
            Id = Guid.NewGuid(),
            PaymentProvider = PaymentProvider.Stripe,
            EntityType = PaymentEntityType.Organization,
            EntityId = entityId,
            InvoiceId = null,
            StripePaymentIntentId = intent.Id,
            AmountPaid = intent.Amount,
            Currency = intent.Currency ?? "usd",
            Status = intent.Status ?? "failed",
            EventType = StripeEvents.PaymentIntentFailed,
            PaidAt = DateTime.UtcNow,
            RawEventJson = "{}"
        });
    }

    private async Task HandleChargeRefundedAsync(Charge charge, string eventType)
    {
        Guid entityId = Guid.Empty;
        if (!string.IsNullOrWhiteSpace(charge.PaymentIntentId))
        {
            var piService = new PaymentIntentService();
            try
            {
                var pi = await piService.GetAsync(charge.PaymentIntentId);
                if (pi.Metadata.TryGetValue("invoiceId", out var refundInvoiceId) &&
                    Guid.TryParse(refundInvoiceId, out var parsedId))
                {
                    var invoiceResult = await _invoiceService.GetInvoiceByIdAsync(parsedId);
                    if (invoiceResult.IsSuccess)
                        entityId = invoiceResult.Value.OrganizationId;
                }
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Could not resolve PaymentIntent {PaymentIntentId} for charge refund org lookup", charge.PaymentIntentId);
            }
        }

        await _paymentHistoryService.LogAsync(new JobFlow.Domain.Models.PaymentHistory
        {
            Id = Guid.NewGuid(),
            PaymentProvider = PaymentProvider.Stripe,
            EntityType = PaymentEntityType.Organization,
            EntityId = entityId,
            InvoiceId = null,
            StripePaymentIntentId = charge.PaymentIntentId,
            AmountPaid = -charge.AmountRefunded,
            Currency = charge.Currency ?? "usd",
            Status = charge.Status ?? "refunded",
            EventType = eventType,
            PaidAt = DateTime.UtcNow,
            RawEventJson = "{}"
        });
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
        var subResult = await _subscriptionRecordService.GetByProviderIdAsync(subscription.Id);
        if (!subResult.IsSuccess)
            return;

        var previousStatus = subResult.Value.Status;

        subResult.Value.Status = subscription.Status;
        subResult.Value.ProviderPriceId = subscription.Items.Data.First().Price.Id;
        subResult.Value.PlanName = subscription.Items?.Data?.FirstOrDefault()?.Price?.Metadata?.GetValueOrDefault("plan-name")
                       ?? subscription.Items?.Data?.FirstOrDefault()?.Price?.Nickname
                       ?? subResult.Value.PlanName;

        await _subscriptionRecordService.UpdateAsync(subResult.Value);

        if (TryGetSubscriptionOwnerMetadata(subscription, out var ownerId, out var ownerType)
            && ownerType.Equals(PaymentEntityType.Organization.ToString(), StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(ownerId, out var organizationId))
        {
            await _organizationService.UpdateSubscriptionStateAsync(
                organizationId,
                subscription.Status,
                subscription.Items?.Data?.FirstOrDefault()?.Price?.Metadata?.GetValueOrDefault("plan-name")
                ?? subscription.Items?.Data?.FirstOrDefault()?.Price?.Nickname,
                ResolveSubscriptionExpiryUtc(subscription)
            );
        }

        if (!string.IsNullOrWhiteSpace(subscription.CustomerId))
        {
            await _paymentProfileService.SetDelinquentByProviderCustomerAsync(
                PaymentProvider.Stripe,
                subscription.CustomerId,
                IsDelinquentStatus(subscription.Status));
        }

        if (!IsSubscriptionCompleteStatus(previousStatus)
            && IsSubscriptionCompleteStatus(subscription.Status)
            && TryGetSubscriptionOwnerMetadata(subscription, out var welcomeOwnerId, out var welcomeOwnerType))
        {
            await TrySendOrganizationWelcomeAsync(welcomeOwnerId, welcomeOwnerType, subscription.Status, subscription.Id, StripeEvents.CustomerSubscriptionUpdated);
        }
    }

    private async Task HandleSubscriptionCreatedAsync(Subscription subscription)
    {
        if (!subscription.Metadata.TryGetValue("ownerId", out var ownerId) ||
            !subscription.Metadata.TryGetValue("ownerType", out var ownerType) ||
            !subscription.Metadata.TryGetValue("customerId", out var paymentCustomerId))
        {
            _logger.LogWarning(
                "Stripe subscription.created missing required metadata (ownerId/ownerType/customerId). SubscriptionId={SubscriptionId}",
                subscription.Id);
            return;
        }

        var priceId = subscription.Items?.Data?.FirstOrDefault()?.Price?.Id;
        var planName = subscription.Items?.Data?.FirstOrDefault()?.Price?.Metadata?.GetValueOrDefault("plan-name");

        if (string.IsNullOrWhiteSpace(priceId))
        {
            _logger.LogWarning("Stripe subscription missing price id. SubscriptionId={SubscriptionId}", subscription.Id);
            return;
        }

        if (string.IsNullOrWhiteSpace(planName))
        {
            planName = "Unknown";
        }

        var existingSubscription = await _subscriptionRecordService.GetByProviderIdAsync(subscription.Id);

        var paymentProfileResult = await _paymentProfileService.UpsertAsync(
            Guid.Parse(ownerId),
            Enum.Parse<PaymentEntityType>(ownerType),
            PaymentProvider.Stripe,
            paymentCustomerId
        );

        await _subscriptionRecordService.CreateAsync(
            paymentProfileResult.Value.Id,
            subscription.Id,
            priceId,
            subscription.Status,
            planName
        );

        if (Enum.TryParse<PaymentEntityType>(ownerType, true, out var parsedOwnerType)
            && parsedOwnerType == PaymentEntityType.Organization
            && Guid.TryParse(ownerId, out var organizationId))
        {
            var periodEndUtc = ResolveSubscriptionExpiryUtc(subscription);
            await _organizationService.UpdateSubscriptionStateAsync(organizationId, subscription.Status, planName, periodEndUtc);
            await _paymentProfileService.SetDelinquentByProviderCustomerAsync(
                PaymentProvider.Stripe,
                paymentCustomerId,
                IsDelinquentStatus(subscription.Status));
        }

        if (existingSubscription.IsFailure)
        {
            await TrySendOrganizationWelcomeAsync(ownerId, ownerType, subscription.Status, subscription.Id, StripeEvents.SubscriptionCreated);
        }
    }

    private async Task TrySendOrganizationWelcomeAsync(
        string ownerIdRaw,
        string ownerTypeRaw,
        string? subscriptionStatus,
        string subscriptionId,
        string sourceEvent)
    {
        if (!IsSubscriptionCompleteStatus(subscriptionStatus))
            return;

        if (!Enum.TryParse<PaymentEntityType>(ownerTypeRaw, true, out var ownerType)
            || ownerType != PaymentEntityType.Organization)
            return;

        if (!Guid.TryParse(ownerIdRaw, out var organizationId))
        {
            _logger.LogWarning(
                "Unable to parse organization owner id for welcome notification. SubscriptionId={SubscriptionId}, OwnerId={OwnerId}, EventType={EventType}",
                subscriptionId,
                ownerIdRaw,
                sourceEvent);
            return;
        }

        var orgResult = await _organizationService.GetOrganiztionById(organizationId);
        if (orgResult.IsFailure)
        {
            _logger.LogWarning(
                "Organization not found for welcome notification. OrganizationId={OrganizationId}, SubscriptionId={SubscriptionId}, EventType={EventType}",
                organizationId,
                subscriptionId,
                sourceEvent);
            return;
        }

        await _notificationService.SendOrganizationWelcomeNotificationAsync(orgResult.Value);
    }

    private static bool IsSubscriptionCompleteStatus(string? status)
    {
        return string.Equals(status, "active", StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, "trialing", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDelinquentStatus(string? status)
    {
        return string.Equals(status, "past_due", StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, "unpaid", StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, "incomplete", StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, "incomplete_expired", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime? ResolveSubscriptionExpiryUtc(Subscription subscription)
    {
        var periodEnd = subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd
                        ?? subscription.CancelAt
                        ?? subscription.EndedAt
                        ?? subscription.TrialEnd;

        return periodEnd?.ToUniversalTime();
    }

    private static bool TryGetSubscriptionOwnerMetadata(
        Subscription subscription,
        out string ownerId,
        out string ownerType)
    {
        ownerId = string.Empty;
        ownerType = string.Empty;

        if (subscription.Metadata == null)
            return false;

        if (!subscription.Metadata.TryGetValue("ownerId", out var ownerIdValue)
            || !subscription.Metadata.TryGetValue("ownerType", out var ownerTypeValue))
            return false;

        ownerId = ownerIdValue ?? string.Empty;
        ownerType = ownerTypeValue ?? string.Empty;

        return !string.IsNullOrWhiteSpace(ownerId)
               && !string.IsNullOrWhiteSpace(ownerType);
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

    private async Task HandleDisputeAsync(Dispute dispute, string eventType)
    {
        var customerId = dispute.Charge?.CustomerId;
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            await _paymentProfileService.SetDelinquentByProviderCustomerAsync(PaymentProvider.Stripe, customerId, true);
        }

        await _paymentHistoryService.LogAsync(new JobFlow.Domain.Models.PaymentHistory
        {
            Id = Guid.NewGuid(),
            PaymentProvider = PaymentProvider.Stripe,
            EntityType = PaymentEntityType.Organization,
            EntityId = Guid.Empty,
            StripePaymentIntentId = dispute.PaymentIntent?.Id,
            CustomerId = customerId,
            AmountPaid = dispute.Amount,
            Currency = dispute.Currency ?? "usd",
            Status = dispute.Status,
            EventType = eventType,
            PaidAt = DateTime.UtcNow,
            RawEventJson = "{}"
        });
    }

    private async Task HandleLedgerEventAsync(string eventType, string providerId, long amount, string currency, string status)
    {
        await _paymentHistoryService.LogAsync(new JobFlow.Domain.Models.PaymentHistory
        {
            Id = Guid.NewGuid(),
            PaymentProvider = PaymentProvider.Stripe,
            EntityType = PaymentEntityType.Organization,
            EntityId = Guid.Empty,
            StripePaymentIntentId = providerId,
            AmountPaid = amount,
            Currency = currency ?? "usd",
            Status = status,
            EventType = eventType,
            PaidAt = DateTime.UtcNow,
            RawEventJson = "{}"
        });
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