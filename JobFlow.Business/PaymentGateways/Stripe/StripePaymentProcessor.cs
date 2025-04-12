using global::Stripe.Checkout;
using global::Stripe;
using JobFlow.Business.PaymentGateways.SharedModels;
using JobFlow.Infrastructure.DI;


namespace JobFlow.Business.PaymentGateways.Stripe
{

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
                ReturnUrl = $"http://localhost:4200/dashboard/stripe-success/{accountId}",
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
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.ProductName,
                        },
                        UnitAmount = (long)(request.Amount * 100),
                    },
                    Quantity = request.Quantity,
                },
            },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    ApplicationFeeAmount = (long)(request.ApplicationFeeAmount * 100),
                },
                Mode = "payment",
                SuccessUrl = request.SuccessUrl,
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
            string customerId = request.StripeCustomerId;
            try
            {
                // If the user is subscribing for the first time, create a new Stripe customer
                if (string.IsNullOrEmpty(customerId))
                {
                    customerId = await CreateStripeCustomerAsync(request.Email);
                }

                var options = new SessionCreateOptions
                {
                    Mode = "subscription",
                    LineItems = new List<SessionLineItemOptions>
            {
            new()
            {
                Price = "price_1RCsrMEKX7voND9YOTQ6qU3g", //"request.StripePriceId",
                Quantity = request.Quantity
            }
            },
                    SuccessUrl = "http://localhost:4200/", //request.SuccessUrl,
                    CancelUrl = "http://localhost:4200/",//request.CancelUrl,
                    Customer = request.StripeCustomerId,
                    Metadata = new Dictionary<string, string>
            {
            { "paymentProfileId", request.PaymentProfileId.ToString() }
            }
                };

                var sessionService = new SessionService();
                var session = await sessionService.CreateAsync(options);
                return session.Url;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<string> CreateStripeCustomerAsync(string email)
        {
            var options = new CustomerCreateOptions
            {
                Email = email,
                // Add any other information you need for the customer
            };
            var service = new CustomerService();
            var customer = await service.CreateAsync(options);

            return customer.Id;  // This is the StripeCustomerId
        }

    }

}
