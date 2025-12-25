using JobFlow.Business.Models;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Infrastructure.ExternalServices.ReCAPTCHA;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/email/")]
public class EmailController : ControllerBase
{
    [HttpPost]
    [Route("newsletter")]
    public async Task<IActionResult> SubscribeToNewsletter(
        [FromBody] NewsletterSubscriptionRequest request,
        [FromServices] IBrevoService brevoService,
        [FromServices] IReCAPTCHAService reCAPTCHAService)
    {
        var isHuman = await reCAPTCHAService.VerifyTokenAsync(request.CaptchaToken);
        if (!isHuman) return BadRequest("reCaptcha validation failed.");

        var success = await brevoService.AddContactAsync(request.Email, request.ListId);
        return success
            ? Ok(new { message = "Added to contact list." })
            : StatusCode(500, new { message = "Failed to subscribe" });
    }

    [HttpPost]
    [Route("send-contact-form")]
    public async Task<IActionResult> SendContactForm(
        [FromBody] ContactFormRequest request,
        [FromServices] IBrevoService brevoService,
        [FromServices] IReCAPTCHAService reCAPTCHAService)
    {
        var isHuman = await reCAPTCHAService.VerifyTokenAsync(request.CaptchaToken);
        if (!isHuman) return BadRequest("reCaptcha validation failed.");
        var success = await brevoService.SendContactEmailAsync(request);
        return success
            ? Ok(new { message = "Contact form submitted." })
            : StatusCode(500, new { message = "Failed to submit." });
    }
}