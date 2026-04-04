using JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces;
using JobFlow.Business.DI;
using JobFlow.Business.Extensions;
using JobFlow.Business.PaymentGateways;
using JobFlow.Business.PaymentGateways.SharedModels;
using JobFlow.Domain.Enums;
using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;
using Stripe;
using Stripe.Checkout;

namespace JobFlow.Infrastructure.PaymentGateways.Stripe;

[ScopedService]
public class StripePaymentProcessor : IPaymentProcessor, IPaymentOperationsProcessor, ISubscriptionOperationsProcessor, IConnectedAccountProcessor
{
    private readonly IPaymentSettings _paymentSettings;
    private readonly IStripeSettings _stripeSettings;
    public StripePaymentProcessor(IPaymentSettings paymentSettings, IStripeSettings stripeSettings)
    {
        _paymentSettings = paymentSettings;
        _stripeSettings = stripeSettings;
    }
    public async Task<string> CreateConnectedAccountAsync()
    {
        var service = new AccountService();
        var account = await service.CreateAsync(new AccountCreateOptions
        {
            Type = "express",
            Country = "US",
            Capabilities = new AccountCapabilitiesOptions
            {
                CardPayments = new AccountCapabilitiesCardPaymentsOptions
                {
                    Requested = true
                },
                Transfers = new AccountCapabilitiesTransfersOptions
                {
                    Requested = true
                }
            },

            Settings = new AccountSettingsOptions
            {
                Payouts = new AccountSettingsPayoutsOptions
                {
                    Schedule = new AccountSettingsPayoutsScheduleOptions
                    {
                        Interval = "daily"
                    }
                }
            }
        });

        return account.Id;
    }

    public async Task<string> GenerateAccountLinkAsync(string accountId, string? returnUrl = null, string? refreshUrl = null)
    {
        var resolvedReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
            ? _stripeSettings.ReturnUrl
            : returnUrl;
        var resolvedRefreshUrl = string.IsNullOrWhiteSpace(refreshUrl)
            ? (string.IsNullOrWhiteSpace(_stripeSettings.RefreshUrl)
                ? resolvedReturnUrl
                : _stripeSettings.RefreshUrl)
            : refreshUrl;

        var service = new AccountLinkService();
        var accountLink = await service.CreateAsync(new AccountLinkCreateOptions
        {
            Account = accountId,
            ReturnUrl = resolvedReturnUrl,
            RefreshUrl = resolvedRefreshUrl,
            Type = "account_onboarding"
        });

        return accountLink.Url;
    }
    public async Task<PaymentSessionResult> CreatePaymentIntentAsync(
        PaymentSessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ConnectedAccountId))
            throw new InvalidOperationException("Connected account is required.");

        var amountInCents =
            request.Amount?.ToCents()
            ?? throw new InvalidOperationException("Payment amount is required.");
        long applicationFee = _paymentSettings.ApplicationFee.ToCents();
        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInCents,
            Currency = "usd",

            AutomaticPaymentMethods = new()
            {
                Enabled = true
            },

            ApplicationFeeAmount = applicationFee,

            TransferData = new PaymentIntentTransferDataOptions
            {
                Destination = request.ConnectedAccountId
            },

            Metadata = new Dictionary<string, string>
            {
                { "invoiceId", request.InvoiceId!.Value.ToString() }
            }
        };

        var service = new PaymentIntentService();

        // ⚠️ IMPORTANT: NO StripeAccount header here
        var paymentIntent = await service.CreateAsync(options);

        return new PaymentSessionResult
        {
            ClientSecret = paymentIntent.ClientSecret,
            ProviderPaymentId = paymentIntent.Id
        };
    }

    public async Task<PaymentSessionResult> CreateDepositPaymentAsync(PaymentSessionRequest request)
    {
        request.Amount = request.DepositAmount ?? request.Amount;
        return await CreatePaymentIntentAsync(request);
    }

    public async Task<PaymentOperationResult> RefundPaymentAsync(PaymentRefundRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderPaymentId))
            throw new InvalidOperationException("Provider payment id is required.");

        var refundService = new RefundService();
        var refund = await refundService.CreateAsync(new RefundCreateOptions
        {
            PaymentIntent = request.ProviderPaymentId,
            Amount = request.Amount.ToCents(),
            Reason = request.Reason?.ToLowerInvariant() switch
            {
                "duplicate" => "duplicate",
                "fraudulent" => "fraudulent",
                _ => "requested_by_customer"
            }
        });

        return new PaymentOperationResult
        {
            Success = refund.Status == "succeeded",
            ProviderPaymentId = refund.Id,
            Amount = request.Amount,
            Currency = request.Currency,
            Message = refund.Status
        };
    }

    public async Task<PaymentOperationResult> AdjustPaymentAsync(PaymentAdjustmentRequest request)
    {
        if (request.AdjustmentAmount < 0)
        {
            return await RefundPaymentAsync(new PaymentRefundRequest
            {
                ProviderPaymentId = request.ProviderPaymentId,
                Amount = decimal.Abs(request.AdjustmentAmount),
                Currency = request.Currency,
                Reason = request.Reason,
                ConnectedAccountId = request.ConnectedAccountId
            });
        }

        return new PaymentOperationResult
        {
            Success = false,
            Message = "Positive adjustments require creating a new charge or deposit."
        };
    }

    public async Task<PaymentOperationResult> CancelSubscriptionAsync(string providerSubscriptionId)
    {
        if (string.IsNullOrWhiteSpace(providerSubscriptionId))
            throw new InvalidOperationException("Provider subscription id is required.");

        var subscriptionService = new SubscriptionService();
        var updated = await subscriptionService.UpdateAsync(providerSubscriptionId.Trim(), new SubscriptionUpdateOptions
        {
            CancelAtPeriodEnd = true
        });

        var planName = updated.Items?.Data?.FirstOrDefault()?.Price?.Metadata?.GetValueOrDefault("plan-name")
            ?? updated.Items?.Data?.FirstOrDefault()?.Price?.Nickname;
        var periodEndUtc = ResolveSubscriptionExpiryUtc(updated);

        return new PaymentOperationResult
        {
            Success = true,
            ProviderPaymentId = updated.Id,
            Message = updated.Status,
            SubscriptionStatus = "canceled",
            SubscriptionPlanName = planName,
            ProviderPriceId = updated.Items?.Data?.FirstOrDefault()?.Price?.Id,
            SubscriptionExpiresAtUtc = periodEndUtc
        };
    }

    public async Task<PaymentOperationResult> ChangeSubscriptionPlanAsync(string providerSubscriptionId, string providerPriceId)
    {
        if (string.IsNullOrWhiteSpace(providerSubscriptionId))
            throw new InvalidOperationException("Provider subscription id is required.");

        if (string.IsNullOrWhiteSpace(providerPriceId))
            throw new InvalidOperationException("Provider price id is required.");

        var subscriptionService = new SubscriptionService();
        var existing = await subscriptionService.GetAsync(providerSubscriptionId.Trim());
        var itemId = existing.Items?.Data?.FirstOrDefault()?.Id;
        if (string.IsNullOrWhiteSpace(itemId))
            throw new InvalidOperationException("No subscription item found to update.");

        var updated = await subscriptionService.UpdateAsync(providerSubscriptionId.Trim(), new SubscriptionUpdateOptions
        {
            ProrationBehavior = "create_prorations",
            Items = new List<SubscriptionItemOptions>
            {
                new()
                {
                    Id = itemId,
                    Price = providerPriceId.Trim()
                }
            }
        });

        return new PaymentOperationResult
        {
            Success = !string.IsNullOrWhiteSpace(updated.Id),
            ProviderPaymentId = updated.Id,
            Message = updated.Status,
            SubscriptionStatus = updated.Status,
            SubscriptionPlanName = updated.Items?.Data?.FirstOrDefault()?.Price?.Metadata?.GetValueOrDefault("plan-name")
                                   ?? updated.Items?.Data?.FirstOrDefault()?.Price?.Nickname,
            ProviderPriceId = updated.Items?.Data?.FirstOrDefault()?.Price?.Id,
            SubscriptionExpiresAtUtc = ResolveSubscriptionExpiryUtc(updated)
        };
    }

    private static DateTime? ResolveSubscriptionExpiryUtc(Subscription subscription)
    {
        var periodEnd = subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd
                        ?? subscription.CancelAt
                        ?? subscription.EndedAt
                        ?? subscription.TrialEnd;

        return periodEnd?.ToUniversalTime();
    }

    public async Task<string> CreateSubscriptionCheckoutSessionAsync(PaymentSessionRequest request)
    {
        var customerId = request.StripeCustomerId;
        // If the user is subscribing for the first time, create a new Stripe customer
        if (string.IsNullOrEmpty(customerId))
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new InvalidOperationException("Customer email is required for subscription checkout.");

            customerId = await CreateStripeCustomerAsync(request.Email);
        }

        if (string.IsNullOrWhiteSpace(customerId))
            throw new InvalidOperationException("Stripe customer id is required for subscription checkout.");

        var resolvedCustomerId = request.StripeCustomerId ?? customerId
            ?? throw new InvalidOperationException("Stripe customer id is required for subscription checkout.");
        var ownerId = request.OrgId?.ToString()
            ?? throw new InvalidOperationException("Organization id is required for subscription checkout.");

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Price = request.StripePriceId,
                    Quantity = request.Quantity
                }
            },
            SuccessUrl = BuildSubscriptionSuccessUrl(
                request.SuccessUrl,
                ownerId,
                "{CHECKOUT_SESSION_ID}"),
            CancelUrl = request.CancelUrl,
            Customer = customerId,
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "ownerId", ownerId },
                    { "ownerType", PaymentEntityType.Organization.ToString() },
                    { "customerId", resolvedCustomerId }
                }
            },
            Metadata = new Dictionary<string, string>
            {
                { "ownerId", ownerId },
                { "ownerType", PaymentEntityType.Organization.ToString() },
                { "customerId", resolvedCustomerId }
            }
        };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(options);
        return session.Url;
    }

    public async Task<string> CreateStripeCustomerAsync(string email)
    {
        var options = new CustomerCreateOptions
        {
            Email = email
        };
        var service = new CustomerService();
        var customer = await service.CreateAsync(options);

        if (string.IsNullOrWhiteSpace(customer.Id))
            throw new InvalidOperationException("Stripe customer id was not returned.");

        return customer.Id;
    }

    private static string BuildSubscriptionSuccessUrl(
        string? baseUrl,
        string organizationId,
        string sessionIdPlaceholder)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Success URL is required for subscription checkout.");

        var urlWithoutFragment = baseUrl;
        var fragment = string.Empty;
        var fragmentIndex = baseUrl.IndexOf('#');
        if (fragmentIndex >= 0)
        {
            urlWithoutFragment = baseUrl[..fragmentIndex];
            fragment = baseUrl[fragmentIndex..];
        }

        var queryIndex = urlWithoutFragment.IndexOf('?');
        var path = queryIndex >= 0 ? urlWithoutFragment[..queryIndex] : urlWithoutFragment;
        var query = queryIndex >= 0 ? urlWithoutFragment[(queryIndex + 1)..] : string.Empty;

        var parts = query
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(p =>
                !p.StartsWith("organizationId=", StringComparison.OrdinalIgnoreCase)
                && !p.StartsWith("session_id=", StringComparison.OrdinalIgnoreCase))
            .ToList();

        parts.Add($"organizationId={Uri.EscapeDataString(organizationId)}");
        parts.Add($"session_id={sessionIdPlaceholder}");

        return $"{path}?{string.Join("&", parts)}{fragment}";
    }
}