using JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces;
using JobFlow.Business.DI;
using JobFlow.Business.Extensions;
using JobFlow.Business.PaymentGateways;
using JobFlow.Business.PaymentGateways.SharedModels;
using JobFlow.Domain.Enums;
using Stripe;
using Stripe.Checkout;

namespace JobFlow.Infrastructure.PaymentGateways.Stripe;

[ScopedService]
public class StripePaymentProcessor : IPaymentProcessor, IPaymentOperationsProcessor, IConnectedAccountProcessor
{
    private readonly IPaymentSettings _paymentSettings;
    public StripePaymentProcessor(IPaymentSettings paymentSettings)
    {
        _paymentSettings = paymentSettings;
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

    public async Task<string> GenerateAccountLinkAsync(string accountId)
    {
        var service = new AccountLinkService();
        var accountLink = await service.CreateAsync(new AccountLinkCreateOptions
        {
            Account = accountId,
            ReturnUrl = "http://localhost:4200/admin",
            RefreshUrl = $"http://localhost:4200/dashboard/stripe-failed/{accountId}",
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
        long applicationFee = 75L;
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

    public async Task<string> CreateSubscriptionCheckoutSessionAsync(PaymentSessionRequest request)
    {
        var customerId = request.StripeCustomerId;
        // If the user is subscribing for the first time, create a new Stripe customer
        if (string.IsNullOrEmpty(customerId)) customerId = await CreateStripeCustomerAsync(request.Email);

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
            SuccessUrl = $"{request.SuccessUrl}?organizationId={request.OrgId}&session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = request.CancelUrl,
            Customer = customerId,
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "ownerId", request.OrgId.ToString() },
                    { "ownerType", PaymentEntityType.Organization.ToString() },
                    { "customerId", request.StripeCustomerId ?? customerId }
                }
            },
            Metadata = new Dictionary<string, string>
            {
                { "ownerId", request.OrgId.ToString() },
                { "ownerType", PaymentEntityType.Organization.ToString() },
                { "customerId", customerId }
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

        return customer.Id;
    }
}