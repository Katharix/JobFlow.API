using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace JobFlow.API.Controllers
{
    [Route("api/stripe/")]
    [ApiController]
    public class StripePaymentController : ControllerBase
    {
        [HttpPost, Route("create")]
        public IActionResult CreateAccount()
        {
            try
            {
                var service = new AccountService();
                var options = new AccountCreateOptions();
                Account account = service.Create(options);
                return Ok(new { accountId = account.Id });
            }
            catch (StripeException stripeEx)
            {
                return StatusCode(500, new { error = stripeEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpPost]
        public ActionResult Create([FromBody] AccountLinkPostBody accountLinkPostBody)
        {
            try
            {
                var connectedAccountId = accountLinkPostBody.Account;
                var service = new AccountLinkService();

                AccountLink accountLink = service.Create(
                    new AccountLinkCreateOptions
                    {
                        Account = connectedAccountId,
                        ReturnUrl = $"http://localhost/return/{connectedAccountId}",
                        RefreshUrl = $"http://localhost/refresh/{connectedAccountId}",
                        Type = "account_onboarding",
                    }
                );

                return Ok(new { url = accountLink.Url });
            }
            catch (Exception ex)
            {
                Console.Write("An error occurred when calling the Stripe API to create an account link:  " + ex.Message);
                Response.StatusCode = 500;
                return Ok(new { error = ex.Message });
            }
        }

        [HttpPost, Route("create-checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession()
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
                            Name = "T-shirt",
                        },
                        UnitAmount = 1000, // Amount in cents ($10.00)
                    },
                    Quantity = 1,
                },
            },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    ApplicationFeeAmount = 75, // Platform fee in cents ($1.23)
                },
                Mode = "payment",
                SuccessUrl = "https://example.com/success?session_id={CHECKOUT_SESSION_ID}",
            };

            var requestOptions = new RequestOptions
            {
                StripeAccount = "{{CONNECTED_ACCOUNT_ID}}", // Set this dynamically if needed
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options, requestOptions);

            return Ok(new { url = session.Url });
        }
    }
    public class AccountLinkPostBody
    {
        public string Account { get; set; }
    }
}

