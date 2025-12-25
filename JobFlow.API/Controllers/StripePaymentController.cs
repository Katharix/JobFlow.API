using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace JobFlow.API.Controllers;

[Route("api/stripe/")]
[ApiController]
public class StripePaymentController : ControllerBase
{
    [HttpPost]
    [Route("create")]
    public IActionResult CreateAccount()
    {
        try
        {
            var service = new AccountService();
            var options = new AccountCreateOptions
            {
                Type = "express",
                Country = "US",
                Capabilities = new AccountCapabilitiesOptions
                {
                    CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                }
            };

            var account = service.Create(options);

            // Store account.Id in your DB here

            return Ok(new { accountId = account.Id });
        }
        catch (StripeException stripeEx)
        {
            return StatusCode(500, new { error = stripeEx.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpPost]
    [Route("generate-account-link")]
    public ActionResult GenerateAccountLink([FromBody] AccountLinkPostBody accountLinkPostBody)
    {
        try
        {
            var connectedAccountId = accountLinkPostBody.Account;
            var service = new AccountLinkService();

            var accountLink = service.Create(new AccountLinkCreateOptions
            {
                Account = connectedAccountId,
                ReturnUrl = $"http://localhost:4200/dashboard/stripe-success/{connectedAccountId}",
                RefreshUrl = $"http://localhost:4200/dashboard/stripe-failed/{connectedAccountId}",
                Type = "account_onboarding"
            });

            return Ok(new { url = accountLink.Url });
        }
        catch (Exception ex)
        {
            Console.WriteLine("Stripe error: " + ex.Message);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    [Route("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession()
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
                            Name = "T-shirt"
                        },
                        UnitAmount = 1000 // Amount in cents ($10.00)
                    },
                    Quantity = 1
                }
            },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                ApplicationFeeAmount = 75 // Platform fee in cents ($1.23)
            },
            Mode = "payment",
            SuccessUrl = "https://example.com/success?session_id={CHECKOUT_SESSION_ID}"
        };

        var requestOptions = new RequestOptions
        {
            StripeAccount = "{{CONNECTED_ACCOUNT_ID}}" // Set this dynamically if needed
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, requestOptions);

        return Ok(new { url = session.Url });
    }
}

public class AccountLinkPostBody
{
    public string Account { get; set; }
}