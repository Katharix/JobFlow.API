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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Net.Http.Json;
using System.Text.Json;
using Stripe;
using CreateSubscriptionRequest = JobFlow.Business.Models.DTOs.CreateSubscriptionRequest;
using Event = Stripe.Event;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/payments/")]
[Authorize]
public class PaymentController : ControllerBase
{
    private const string SquareStatePurpose = "SquareOAuthState";
    private static readonly TimeSpan SquareStateLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan WebhookTimestampTolerance = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan WebhookReplayWindow = TimeSpan.FromHours(24);

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
    private readonly IDataProtector _squareStateProtector;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<PaymentController> _logger;

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
        IFrontendSettings frontEndSettings,
        IDataProtectionProvider dataProtectionProvider,
        IDistributedCache distributedCache,
        ILogger<PaymentController> logger)
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
        _squareStateProtector = dataProtectionProvider.CreateProtector(SquareStatePurpose);
        _distributedCache = distributedCache;
        _logger = logger;
    }

    [HttpPost("checkout")]
    [EnableRateLimiting("payment-sensitive")]
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
    [Authorize(Policy = "OrganizationAdminOnly")]
    [EnableRateLimiting("payment-sensitive")]
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
            var onboardingUrl = BuildSquareOnboardingUrl(orgId);
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
    [Authorize(Policy = "OrganizationAdminOnly")]
    [EnableRateLimiting("payment-sensitive")]
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
    [Authorize(Policy = "OrganizationAdminOnly")]
    [EnableRateLimiting("payment-sensitive")]
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
    [Authorize(Policy = "OrganizationAdminOnly")]
    [EnableRateLimiting("payment-sensitive")]
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
    [Authorize(Policy = "OrganizationAdminOnly")]
    [EnableRateLimiting("payment-sensitive")]
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
    [Authorize(Policy = "OrganizationAdminOnly")]
    [EnableRateLimiting("payment-sensitive")]
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
    [Authorize(Policy = "OrganizationAdminOnly")]
    [EnableRateLimiting("payment-sensitive")]
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
    [Authorize(Policy = "OrganizationAdminOnly")]
    [EnableRateLimiting("payment-sensitive")]
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
    [Authorize(Policy = "OrganizationAdminOnly")]
    [EnableRateLimiting("payment-sensitive")]
    public async Task<IActionResult> SetDefaultPaymentMethod([FromBody] SetDefaultPaymentMethodRequest request)
    {
        var result = await _paymentProfileService.SetDefaultPaymentMethodAsync(
            request.ProfileId,
            request.PaymentMethodId
        );

        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
    

    [HttpPost("webhook")]
    [AllowAnonymous]
    [EnableRateLimiting("webhook")]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _stripeSettings.WebhookKey,
                (long)WebhookTimestampTolerance.TotalSeconds
            );

            if (string.IsNullOrWhiteSpace(stripeEvent.Id))
                return BadRequest();

            if (await IsReplayAsync(GetStripeReplayCacheKey(stripeEvent.Id), HttpContext.RequestAborted))
            {
                _logger.LogWarning("Stripe webhook replay detected for event {EventId}", stripeEvent.Id);
                return Ok();
            }
        }
        catch (StripeException)
        {
            return BadRequest();
        }

        await _stripeWebhookService.HandleEventAsync(stripeEvent);
        await MarkProcessedAsync(GetStripeReplayCacheKey(stripeEvent.Id), WebhookReplayWindow, HttpContext.RequestAborted);

        return Ok();
    }

    [HttpPost("square/webhook")]
    [AllowAnonymous]
    [EnableRateLimiting("webhook")]
    public async Task<IActionResult> HandleSquareWebhook()
    {
        var rawBody = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["x-square-hmacsha256-signature"].ToString();
        var callbackUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";

        var squareEventId = TryGetStringProperty(rawBody, "event_id");
        var createdAtValue = TryGetStringProperty(rawBody, "created_at");

        if (!string.IsNullOrWhiteSpace(squareEventId)
            && await IsReplayAsync(GetSquareReplayCacheKey(squareEventId), HttpContext.RequestAborted))
        {
            _logger.LogWarning("Square webhook replay detected for event {EventId}", squareEventId);
            return Ok();
        }

        if (DateTimeOffset.TryParse(createdAtValue, out var createdAt))
        {
            var age = DateTimeOffset.UtcNow - createdAt;
            if (age > WebhookTimestampTolerance || age < -TimeSpan.FromMinutes(1))
            {
                _logger.LogWarning("Square webhook timestamp out of tolerance: {CreatedAt}", createdAt);
                return Unauthorized();
            }
        }

        try
        {
            await _squareWebhookService.HandleEventAsync(rawBody, signature, callbackUrl);
            if (!string.IsNullOrWhiteSpace(squareEventId))
                await MarkProcessedAsync(GetSquareReplayCacheKey(squareEventId), WebhookReplayWindow, HttpContext.RequestAborted);
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

        if (!TryReadSquareState(state, out var organizationId))
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

    private string BuildSquareOnboardingUrl(Guid organizationId)
    {
        if (string.IsNullOrWhiteSpace(_squareSettings.ApplicationId))
            throw new InvalidOperationException("Square ApplicationId is not configured.");
        if (string.IsNullOrWhiteSpace(_squareSettings.RedirectUrl))
            throw new InvalidOperationException("Square RedirectUrl is not configured.");

        var nonce = Guid.NewGuid().ToString("N");
        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var rawState = $"{organizationId:N}|{nonce}|{issuedAt}";
        var protectedState = _squareStateProtector.Protect(rawState);
        _distributedCache.SetString(
            GetSquareStateCacheKey(nonce),
            "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = SquareStateLifetime });

        var connectBaseUrl = _hostEnvironment.IsDevelopment()
            ? "https://connect.squareupsandbox.com"
            : "https://connect.squareup.com";

        return $"{connectBaseUrl}/oauth2/authorize?client_id={Uri.EscapeDataString(_squareSettings.ApplicationId)}&response_type=code&scope=PAYMENTS_WRITE+PAYMENTS_READ+ORDERS_READ+SUBSCRIPTIONS_READ+SUBSCRIPTIONS_WRITE&state={Uri.EscapeDataString(protectedState)}&redirect_uri={Uri.EscapeDataString(_squareSettings.RedirectUrl)}";
    }

    private bool TryReadSquareState(string? state, out Guid organizationId)
    {
        organizationId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(state))
            return false;

        string raw;
        try
        {
            raw = _squareStateProtector.Unprotect(state);
        }
        catch
        {
            return false;
        }

        var parts = raw.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            return false;

        if (!Guid.TryParse(parts[0], out organizationId))
            return false;

        var nonce = parts[1];
        if (string.IsNullOrWhiteSpace(nonce))
            return false;

        if (!long.TryParse(parts[2], out var issuedAt))
            return false;

        var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(issuedAt);
        if (age < TimeSpan.Zero || age > SquareStateLifetime)
            return false;

        var cacheKey = GetSquareStateCacheKey(nonce);
        var stateValue = _distributedCache.GetString(cacheKey);
        if (string.IsNullOrWhiteSpace(stateValue))
            return false;

        _distributedCache.Remove(cacheKey);
        return true;
    }

    private static string GetSquareStateCacheKey(string nonce) => $"square-oauth-state:{nonce}";

    private static string? TryGetStringProperty(string json, string propertyName)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty(propertyName, out var value)
                ? value.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> IsReplayAsync(string cacheKey, CancellationToken cancellationToken)
    {
        var value = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
        return !string.IsNullOrWhiteSpace(value);
    }

    private async Task MarkProcessedAsync(string cacheKey, TimeSpan ttl, CancellationToken cancellationToken)
    {
        await _distributedCache.SetStringAsync(
            cacheKey,
            "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);
    }

    private static string GetStripeReplayCacheKey(string eventId) => $"stripe-webhook:{eventId}";
    private static string GetSquareReplayCacheKey(string eventId) => $"square-webhook:{eventId}";

    private string BuildSquareUiRedirectBaseUrl()
    {
        var baseUrl = (_frontEndSettings.BaseUrl ?? "http://localhost:4200").TrimEnd('/');
        return $"{baseUrl}/admin/connectedpayment";
    }
}