using JobFlow.Business.Models.DTOs;
using JobFlow.Business.PaymentGateways;
using JobFlow.Business.PaymentGateways.SharedModels;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using JobFlow.Infrastructure.PaymentGateways;
using JobFlow.Infrastructure.PaymentGateways.Stripe;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/payments/")]
public class PaymentController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly IPaymentProfileService _paymentProfileService;
    private readonly IPaymentProcessorFactory _processorFactory;
    private readonly IStripeWebhookService _stripeWebhookService;
    private readonly ISubscriptionRecordService _subscriptionRecordService;

    public PaymentController(
        IPaymentProcessorFactory processorFactory,
        IOrganizationService organizationService,
        IPaymentProfileService paymentProfileService,
        ISubscriptionRecordService subscriptionRecordService,
        IStripeWebhookService stripeWebhookService)
    {
        _processorFactory = processorFactory;
        _organizationService = organizationService;
        _paymentProfileService = paymentProfileService;
        _subscriptionRecordService = subscriptionRecordService;
        _stripeWebhookService = stripeWebhookService;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] PaymentSessionRequest request)
    {
        var org = await _organizationService.GetOrganiztionById(request.OrgId.Value);
        if (org == null) return NotFound("Organization not found.");

        var processor = _processorFactory.GetProcessor(org.Value.PaymentProvider.ToString());

        string checkoutUrl;
        if (request.Mode == "subscription")
            checkoutUrl = await processor.CreateSubscriptionCheckoutSessionAsync(request);
        else
            checkoutUrl = await processor.CreateCheckoutSessionAsync(request);

        return Ok(new { url = checkoutUrl });
    }


    [HttpPost("{orgId}/create-connected-account")]
    public async Task<IActionResult> CreateConnectedAccount(Guid orgId)
    {
        var org = await _organizationService.GetOrganiztionById(orgId);
        if (org == null) return NotFound();

        var processor = _processorFactory.GetProcessor(org.Value.PaymentProvider.ToString());

        if (processor is IConnectedAccountProcessor connected)
        {
            var accountId = await connected.CreateConnectedAccountAsync();
            if (accountId == null) return NotFound();

            var onboardingUrl = await connected.GenerateAccountLinkAsync(accountId);
            return Ok(new { onboarding = onboardingUrl });
        }

        return BadRequest("This provider does not support connected accounts.");
    }

    // POST: api/payments/{orgId}/profile
    [HttpPost("{orgId}/profile")]
    public async Task<IActionResult> CreatePaymentProfile(Guid orgId, [FromBody] CreatePaymentProfileRequest request)
    {
        var result = await _paymentProfileService.CreateAsync(
            orgId,
            PaymentEntityType.Organization,
            request.Provider,
            request.ProviderCustomerId
        );

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // POST: api/payments/{orgId}/subscription
    [HttpPost("{orgId}/subscription")]
    public async Task<IActionResult> CreateSubscription(Guid orgId, [FromBody] CreateSubscriptionRequest request)
    {
        var result = await _subscriptionRecordService.CreateAsync(
            request.PaymentProfileId,
            request.ProviderSubscriptionId,
            request.ProviderPriceId,
            request.Status ?? "active"
        );

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // POST: api/payments/subscription/cancel
    [HttpPost("subscription/cancel")]
    public async Task<IActionResult> CancelSubscription([FromBody] CancelSubscriptionRequest request)
    {
        var result = await _subscriptionRecordService.CancelAsync(
            request.ProviderSubscriptionId,
            request.CanceledAt
        );

        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    // POST: api/payments/profile/default-method
    [HttpPost("profile/default-method")]
    public async Task<IActionResult> SetDefaultPaymentMethod([FromBody] SetDefaultPaymentMethodRequest request)
    {
        var result = await _paymentProfileService.SetDefaultPaymentMethodAsync(
            request.ProfileId,
            request.PaymentMethodId
        );

        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                "whsec_449239427e6f306629fdd3cf4a2d4e8157b1817c8ae85de887bd76380a12bf9a"
            );
        }
        catch (StripeException e)
        {
            return BadRequest();
        }

        await _stripeWebhookService.HandleEventAsync(stripeEvent);

        return Ok();
    }
}