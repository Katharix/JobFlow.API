using JobFlow.Business.Models;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Infrastructure.ExternalServices.Turnstile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/email/")]
public class EmailController : ControllerBase
{
    [HttpPost]
    [Route("newsletter")]
    public async Task<IActionResult> SubscribeToNewsletter(
        [FromBody] NewsletterSubscriptionRequest request,
        [FromServices] IBrevoService brevoService,
        [FromServices] ICaptchaVerificationService captchaService,
        CancellationToken cancellationToken)
    {
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var verification = await captchaService.VerifyAsync(
            request.CaptchaToken,
            "newsletter-subscribe",
            remoteIp,
            cancellationToken);

        if (!verification.IsValid)
        {
            return BadRequest(new
            {
                message = "Turnstile validation failed.",
                errors = verification.ErrorCodes
            });
        }

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
        [FromServices] ICaptchaVerificationService captchaService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CaptchaToken))
        {
            return BadRequest(new { message = "Captcha token is required." });
        }

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var verification = await captchaService.VerifyAsync(
            request.CaptchaToken,
            "contact-sales",
            remoteIp,
            cancellationToken);

        if (!verification.IsValid)
        {
            return BadRequest(new
            {
                message = "Turnstile validation failed.",
                errors = verification.ErrorCodes
            });
        }
        var success = await brevoService.SendContactEmailAsync(request);
        return success
            ? Ok(new { message = "Contact form submitted." })
            : StatusCode(500, new { message = "Failed to submit." });
    }
}