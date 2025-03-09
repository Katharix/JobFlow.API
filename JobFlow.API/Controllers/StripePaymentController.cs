using Microsoft.AspNetCore.Mvc;
using Stripe;

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
    }
}
