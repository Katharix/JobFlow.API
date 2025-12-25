using JobFlow.Business.DI;
using JobFlow.Business.PaymentGateways;
using JobFlow.Business.PaymentGateways.SharedModels;
using JobFlow.Domain.Enums;
using Stripe;
using Stripe.Checkout;

namespace JobFlow.Infrastructure.PaymentGateways.Stripe;

[ScopedService]
public class StripePaymentProcessor : IPaymentProcessor, IConnectedAccountProcessor
{
    public async Task<string> CreateConnectedAccountAsync()
    {
        var service = new AccountService();
        var account = await service.CreateAsync(new AccountCreateOptions
        {
            Type = "express",
            Country = "US",
            Capabilities = new AccountCapabilitiesOptions
            {
                CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
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
            ReturnUrl = "http://localhost:4200/onboarding",
            RefreshUrl = $"http://localhost:4200/dashboard/stripe-failed/{accountId}",
            Type = "account_onboarding"
        });

        return accountLink.Url;
    }

    public async Task<string> CreateCheckoutSessionAsync(PaymentSessionRequest request)
    {
        var options = new SessionCreateOptions
        {
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.ProductName
                        },
                        UnitAmount = (long)(request.Amount ?? request.DepositAmount) * 100
                    },
                    Quantity = request.Quantity
                }
            },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                ApplicationFeeAmount = (long)(request.ApplicationFeeAmount * 100)
            },
            Mode = "payment",
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl
        };

        var requestOptions = new RequestOptions
        {
            StripeAccount = request.ConnectedAccountId
        };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(options, requestOptions);
        return session.Url;
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