using JobFlow.Business.Models.DTOs;
using JobFlow.Business.PaymentGateways;
using JobFlow.Business.PaymentGateways.SharedModels;
using JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using JobFlow.API.Extensions;
using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;
using JobFlow.Infrastructure.PaymentGateways;
using JobFlow.Infrastructure.PaymentGateways.Square;
using JobFlow.Infrastructure.PaymentGateways.Stripe;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;
using Stripe;
using CreateSubscriptionRequest = JobFlow.Business.Models.DTOs.CreateSubscriptionRequest;
using Event = Stripe.Event;

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
    private readonly IInvoiceService _invoiceService;
    private readonly IStripeSettings _stripeSettings;
    private readonly ISquareSettings _squareSettings;
    private readonly ISquareWebhookService _squareWebhookService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IFrontendSettings _frontEndSettings;

    public PaymentController(
        IPaymentProcessorFactory processorFactory,
        IOrganizationService organizationService,
        IPaymentProfileService paymentProfileService,
        ISubscriptionRecordService subscriptionRecordService,
        IStripeWebhookService stripeWebhookService,
        ISquareWebhookService squareWebhookService,
        IInvoiceService invoiceService,
        IStripeSettings stripeSettings,
        ISquareSettings squareSettings,
        IHostEnvironment hostEnvironment,
        IFrontendSettings frontEndSettings)
    {
        _processorFactory = processorFactory;
        _organizationService = organizationService;
        _paymentProfileService = paymentProfileService;
        _subscriptionRecordService = subscriptionRecordService;
        _stripeWebhookService = stripeWebhookService;
        _squareWebhookService = squareWebhookService;
        _invoiceService = invoiceService;
        _stripeSettings = stripeSettings;
        _squareSettings = squareSettings;
        _hostEnvironment = hostEnvironment;
        _frontEndSettings = frontEndSettings;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] PaymentSessionRequest request)
    {
        var orgId = HttpContext.GetOrganizationId();
        request.OrgId = orgId;

        var org = await _organizationService.GetOrganiztionById(orgId);
        if (org.IsFailure) return NotFound("Organization not found.");

        var processor = _processorFactory.GetProcessor(org.Value.PaymentProvider.ToString());

        string checkoutUrl;
        if (request.Mode == "subscription")
            checkoutUrl = await processor.CreateSubscriptionCheckoutSessionAsync(request);
        else
        {
            request.ConnectedAccountId = org.Value.StripeConnectAccountId;
            var paymentIntent = await processor.CreatePaymentIntentAsync(request);

            return Ok(new
            {
                clientSecret = paymentIntent.ClientSecret,
                url = paymentIntent.RedirectUrl,
                providerPaymentId = paymentIntent.ProviderPaymentId
            });
        }
        return Ok(new { url = checkoutUrl });
    }

    [HttpPost("create-connected-account")]
    public async Task<IActionResult> CreateConnectedAccount(
        [FromQuery] PaymentProvider? provider = null)
    {
        var orgId = HttpContext.GetOrganizationId();
        var orgResult = await _organizationService.GetOrganiztionById(orgId);
        if (orgResult.IsFailure) return NotFound();

        var organization = orgResult.Value;

        // If UI passed a provider (Stripe=1, Square=2), persist it first.
        if (provider.HasValue && organization.PaymentProvider != provider.Value)
        {
            organization.PaymentProvider = provider.Value;
            var updateResult = await _organizationService.UpsertOrganization(organization);
            if (updateResult.IsFailure)
                return BadRequest(updateResult.Error);

            organization = updateResult.Value;
        }

        if (organization.PaymentProvider == PaymentProvider.Square)
        {
            var onboardingUrl = BuildSquareOnboardingUrl(orgId.ToString());
            return Ok(new { onboarding = onboardingUrl });
        }

        var processor = _processorFactory.GetProcessor(organization.PaymentProvider.ToString());

        if (processor is IConnectedAccountProcessor connected)
        {
            var accountId = await connected.CreateConnectedAccountAsync();
            if (string.IsNullOrWhiteSpace(accountId))
                return NotFound("Unable to create connected account.");

            organization.StripeConnectAccountId = accountId;
            organization.IsStripeConnected = true;

            var updatedOrg = await _organizationService.UpsertOrganization(organization);
            if (updatedOrg.IsFailure)
                return BadRequest(updatedOrg.Error);

            var onboardingUrl = await connected.GenerateAccountLinkAsync(accountId);
            return Ok(new { onboarding = onboardingUrl });
        }

        return BadRequest("This provider does not support connected accounts.");
    }

    [HttpPost("link-connected-account")]
    public async Task<IActionResult> LinkConnectedAccount([FromBody] LinkConnectedAccountRequest request)
    {
        var orgId = HttpContext.GetOrganizationId();

        if (string.IsNullOrWhiteSpace(request.AccountId))
            return BadRequest("Account id is required.");

        var orgResult = await _organizationService.GetOrganiztionById(orgId);
        if (orgResult.IsFailure)
            return NotFound(orgResult.Error);

        var organization = orgResult.Value;
        organization.PaymentProvider = request.Provider;

        if (request.Provider == PaymentProvider.Stripe)
        {
            organization.StripeConnectAccountId = request.AccountId;
            organization.IsStripeConnected = true;
            var updated = await _organizationService.UpsertOrganization(organization);
            return updated.IsSuccess ? Ok(new { linked = true }) : BadRequest(updated.Error);
        }

        var profileResult = await _paymentProfileService.UpsertAsync(
            orgId,
            PaymentEntityType.Organization,
            PaymentProvider.Square,
            request.AccountId
        );

        if (profileResult.IsFailure)
            return BadRequest(profileResult.Error);

        var orgUpdate = await _organizationService.UpsertOrganization(organization);
        return orgUpdate.IsSuccess ? Ok(new { linked = true }) : BadRequest(orgUpdate.Error);
    }

    [HttpPost("refund")]
    public async Task<IActionResult> RefundPayment([FromBody] PaymentRefundRequestDto request)
    {
        var orgId = HttpContext.GetOrganizationId();
        var orgResult = await _organizationService.GetOrganiztionById(orgId);
        if (orgResult.IsFailure)
            return NotFound(orgResult.Error);

        var processor = _processorFactory.GetProcessor(request.Provider);
        if (processor is not IPaymentOperationsProcessor ops)
            return BadRequest("Processor does not support refund operations.");

        var result = await ops.RefundPaymentAsync(new PaymentRefundRequest
        {
            ProviderPaymentId = request.ProviderPaymentId,
            Amount = request.Amount,
            Currency = request.Currency,
            Reason = request.Reason,
            ConnectedAccountId = orgResult.Value.StripeConnectAccountId
        });

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("adjust")]
    public async Task<IActionResult> AdjustPayment([FromBody] PaymentAdjustmentRequestDto request)
    {
        var orgId = HttpContext.GetOrganizationId();
        var orgResult = await _organizationService.GetOrganiztionById(orgId);
        if (orgResult.IsFailure)
            return NotFound(orgResult.Error);

        var processor = _processorFactory.GetProcessor(request.Provider);
        if (processor is not IPaymentOperationsProcessor ops)
            return BadRequest("Processor does not support adjustment operations.");

        var result = await ops.AdjustPaymentAsync(new PaymentAdjustmentRequest
        {
            ProviderPaymentId = request.ProviderPaymentId,
            AdjustmentAmount = request.AdjustmentAmount,
            Currency = request.Currency,
            Reason = request.Reason,
            ProductName = request.ProductName,
            InvoiceId = request.InvoiceId,
            ConnectedAccountId = orgResult.Value.StripeConnectAccountId
        });

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("deposit")]
    public async Task<IActionResult> CreateDeposit([FromBody] DepositPaymentRequestDto request)
    {
        var orgId = HttpContext.GetOrganizationId();
        var orgResult = await _organizationService.GetOrganiztionById(orgId);
        if (orgResult.IsFailure)
            return NotFound(orgResult.Error);

        var processor = _processorFactory.GetProcessor(request.Provider);
        if (processor is not IPaymentOperationsProcessor ops)
            return BadRequest("Processor does not support deposit operations.");

        var result = await ops.CreateDepositPaymentAsync(new PaymentSessionRequest
        {
            OrgId = orgId,
            OrganizationClientId = request.OrganizationClientId,
            InvoiceId = request.InvoiceId,
            ProductName = request.ProductName,
            Amount = request.Amount,
            DepositAmount = request.Amount,
            ConnectedAccountId = orgResult.Value.StripeConnectAccountId
        });

        return Ok(new
        {
            clientSecret = result.ClientSecret,
            url = result.RedirectUrl,
            providerPaymentId = result.ProviderPaymentId
        });
    }

    // POST: api/payments/profile
    [HttpPost("profile")]
    public async Task<IActionResult> CreatePaymentProfile([FromBody] CreatePaymentProfileRequest request)
    {
        var orgId = HttpContext.GetOrganizationId();
        var result = await _paymentProfileService.CreateAsync(
            orgId,
            PaymentEntityType.Organization,
            request.Provider,
            request.ProviderCustomerId
        );

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // POST: api/payments/subscription
    [HttpPost("subscription")]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        var result = await _subscriptionRecordService.CreateAsync(
            request.PaymentProfileId,
            request.ProviderSubscriptionId,
            request.ProviderPriceId,
            request.Status ?? "active",
            ""
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
                _stripeSettings.WebhookKey
            );
        }
        catch (StripeException e)
        {
            return BadRequest();
        }

        await _stripeWebhookService.HandleEventAsync(stripeEvent);

        return Ok();
    }

    [HttpPost("square/webhook")]
    public async Task<IActionResult> HandleSquareWebhook()
    {
        var rawBody = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["x-square-hmacsha256-signature"].ToString();
        var callbackUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";

        try
        {
            await _squareWebhookService.HandleEventAsync(rawBody, signature, callbackUrl);
            return Ok();
        }
        catch (InvalidOperationException)
        {
            return Unauthorized();
        }
    }

    [AllowAnonymous]
    [HttpGet("square/callback")]
    public async Task<IActionResult> HandleSquareCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error = null,
        [FromQuery(Name = "error_description")] string? errorDescription = null)
    {
        var uiBase = BuildSquareUiRedirectBaseUrl();

        if (!string.IsNullOrWhiteSpace(error))
            return Redirect($"{uiBase}?provider=square&success=false&error={Uri.EscapeDataString(errorDescription ?? error)}");

        if (string.IsNullOrWhiteSpace(code))
            return Redirect($"{uiBase}?provider=square&success=false&error={Uri.EscapeDataString("Missing Square OAuth code.")}");

        if (string.IsNullOrWhiteSpace(state) || !Guid.TryParse(state, out var organizationId))
            return Redirect($"{uiBase}?provider=square&success=false&error={Uri.EscapeDataString("Invalid Square OAuth state.")}");

        if (string.IsNullOrWhiteSpace(_squareSettings.ApplicationId)
            || string.IsNullOrWhiteSpace(_squareSettings.ClientSecret)
            || string.IsNullOrWhiteSpace(_squareSettings.RedirectUrl))
            return Redirect($"{uiBase}?provider=square&success=false&error={Uri.EscapeDataString("Square OAuth is not configured.")}");

        var connectBaseUrl = _hostEnvironment.IsDevelopment()
            ? "https://connect.squareupsandbox.com"
            : "https://connect.squareup.com";

        using var httpClient = new HttpClient();
        var tokenResponse = await httpClient.PostAsJsonAsync($"{connectBaseUrl}/oauth2/token", new
        {
            client_id = _squareSettings.ApplicationId,
            client_secret = _squareSettings.ClientSecret,
            code,
            grant_type = "authorization_code",
            redirect_uri = _squareSettings.RedirectUrl
        });

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var failureMessage = await tokenResponse.Content.ReadAsStringAsync();
            return Redirect($"{uiBase}?provider=square&success=false&error={Uri.EscapeDataString(failureMessage)}");
        }

        using var tokenDocument = await JsonDocument.ParseAsync(await tokenResponse.Content.ReadAsStreamAsync());
        var merchantId = tokenDocument.RootElement.TryGetProperty("merchant_id", out var merchantElement)
            ? merchantElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(merchantId))
            return Redirect($"{uiBase}?provider=square&success=false&error={Uri.EscapeDataString("Square merchant id was not returned.")}");

        var orgResult = await _organizationService.GetOrganiztionById(organizationId);
        if (orgResult.IsFailure)
            return Redirect($"{uiBase}?provider=square&success=false&error={Uri.EscapeDataString("Organization not found.")}");

        var organization = orgResult.Value;
        organization.PaymentProvider = PaymentProvider.Square;

        var profileResult = await _paymentProfileService.UpsertAsync(
            organizationId,
            PaymentEntityType.Organization,
            PaymentProvider.Square,
            merchantId);

        if (profileResult.IsFailure)
            return Redirect($"{uiBase}?provider=square&success=false&error={Uri.EscapeDataString(profileResult.Error?.ToString() ?? "Unable to save Square payment profile.")}");

        var updateResult = await _organizationService.UpsertOrganization(organization);
        if (updateResult.IsFailure)
            return Redirect($"{uiBase}?provider=square&success=false&error={Uri.EscapeDataString(updateResult.Error?.ToString() ?? "Unable to update organization payment provider.")}");

        return Redirect($"{uiBase}?provider=square&success=true&merchantId={Uri.EscapeDataString(merchantId)}");
    }

    private string BuildSquareOnboardingUrl(string orgState)
    {
        if (string.IsNullOrWhiteSpace(_squareSettings.ApplicationId))
            throw new InvalidOperationException("Square ApplicationId is not configured.");
        if (string.IsNullOrWhiteSpace(_squareSettings.RedirectUrl))
            throw new InvalidOperationException("Square RedirectUrl is not configured.");

        var connectBaseUrl = _hostEnvironment.IsDevelopment()
            ? "https://connect.squareupsandbox.com"
            : "https://connect.squareup.com";

        return $"{connectBaseUrl}/oauth2/authorize?client_id={Uri.EscapeDataString(_squareSettings.ApplicationId)}&response_type=code&scope=PAYMENTS_WRITE+PAYMENTS_READ+ORDERS_READ+SUBSCRIPTIONS_READ+SUBSCRIPTIONS_WRITE&state={Uri.EscapeDataString(orgState)}&redirect_uri={Uri.EscapeDataString(_squareSettings.RedirectUrl)}";
    }

    private string BuildSquareUiRedirectBaseUrl()
    {
        var baseUrl = (_frontEndSettings.BaseUrl ?? "http://localhost:4200").TrimEnd('/');
        return $"{baseUrl}/admin/connectedpayment";
    }
}