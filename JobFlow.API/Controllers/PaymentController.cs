using JobFlow.Business.Models.DTOs;
using JobFlow.Business.PaymentGateways;
using JobFlow.Business.PaymentGateways.SharedModels;
using JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Business.Onboarding;
using JobFlow.Domain.Enums;
using JobFlow.API.Extensions;
using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;
using JobFlow.Infrastructure.PaymentGateways;
using JobFlow.Infrastructure.PaymentGateways.Square;
using JobFlow.Infrastructure.PaymentGateways.Stripe;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Stripe;
using CreateSubscriptionRequest = JobFlow.Business.Models.DTOs.CreateSubscriptionRequest;
using Event = Stripe.Event;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/payments/")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + ",ClientPortalJwt")]
public class PaymentController : ControllerBase
{
    private const string SquareStatePurpose = "SquareOAuthState";
    private const int MaxPageSize = 250;
    private static readonly TimeSpan FinancialSummaryCacheTtl = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan SquareStateLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan WebhookTimestampTolerance = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan WebhookReplayWindow = TimeSpan.FromHours(24);

    private readonly IOrganizationService _organizationService;
    private readonly IPaymentProfileService _paymentProfileService;
    private readonly IPaymentProcessorFactory _processorFactory;
    private readonly IStripeWebhookService _stripeWebhookService;
    private readonly ISubscriptionRecordService _subscriptionRecordService;
    private readonly IInvoiceService _invoiceService;
    private readonly IPaymentHistoryService _paymentHistoryService;
    private readonly IStripeSettings _stripeSettings;
    private readonly ISquareSettings _squareSettings;
    private readonly ISquareWebhookService _squareWebhookService;
    private readonly ISquareTokenEncryptionService _squareTokenEncryption;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IFrontendSettings _frontEndSettings;
    private readonly IOnboardingService _onboardingService;
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
        IPaymentHistoryService paymentHistoryService,
        IStripeSettings stripeSettings,
        ISquareSettings squareSettings,
        ISquareTokenEncryptionService squareTokenEncryption,
        IHostEnvironment hostEnvironment,
        IFrontendSettings frontEndSettings,
        IOnboardingService onboardingService,
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
        _paymentHistoryService = paymentHistoryService;
        _stripeSettings = stripeSettings;
        _squareSettings = squareSettings;
        _squareTokenEncryption = squareTokenEncryption;
        _hostEnvironment = hostEnvironment;
        _frontEndSettings = frontEndSettings;
        _onboardingService = onboardingService;
        _squareStateProtector = dataProtectionProvider.CreateProtector(SquareStatePurpose);
        _distributedCache = distributedCache;
        _logger = logger;
    }

    [HttpPost("checkout")]
    [AllowAnonymous]
    public async Task<IActionResult> Checkout([FromBody] PaymentSessionRequest request)
    {
        if (User?.Identity?.IsAuthenticated != true
            && !string.Equals(request.Mode, "subscription", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized("Anonymous checkout is only allowed for subscription signup.");
        }

        Guid orgId;
        if (User?.Identity?.IsAuthenticated == true)
        {
            try
            {
                orgId = HttpContext.GetOrganizationId();
            }
            catch (UnauthorizedAccessException)
            {
                var requestedOrgId = request.OrgId ?? request.OrganizationId;
                if (!requestedOrgId.HasValue || requestedOrgId.Value == Guid.Empty)
                    return Unauthorized("Organization context is missing.");

                orgId = requestedOrgId.Value;
            }
        }
        else
        {
            var requestedOrgId = request.OrgId ?? request.OrganizationId;
            if (!requestedOrgId.HasValue || requestedOrgId.Value == Guid.Empty)
                return BadRequest("Organization id is required.");

            orgId = requestedOrgId.Value;
        }

        request.OrgId = orgId;

        var org = await _organizationService.GetOrganiztionById(orgId);
        if (org.IsFailure) return NotFound("Organization not found.");

        var organization = org.Value;
        var provider = Enum.IsDefined(typeof(PaymentProvider), organization.PaymentProvider)
            ? organization.PaymentProvider
            : PaymentProvider.Stripe;

        if (request.InvoiceId.HasValue)
        {
            var invoiceResult = await _invoiceService.GetInvoiceByIdAsync(request.InvoiceId.Value);
            if (!invoiceResult.IsSuccess)
                return NotFound("Invoice not found.");

            var invoice = invoiceResult.Value;
            if (invoice.OrganizationId != orgId)
                return Unauthorized();

            if (User?.IsInRole(UserRoles.OrganizationClient) == true)
            {
                Guid clientId;
                try
                {
                    clientId = HttpContext.GetUserId();
                }
                catch (UnauthorizedAccessException)
                {
                    return Unauthorized();
                }

                if (invoice.OrganizationClientId != clientId)
                    return Unauthorized();
            }

            if (!request.Amount.HasValue)
            {
                request.Amount = invoice.BalanceDue;
            }

            if (request.Amount <= 0)
                return BadRequest("Payment amount is required.");

            request.OrganizationId = invoice.OrganizationId;
            request.OrganizationClientId = invoice.OrganizationClientId;
            request.ProductName ??= $"Invoice {invoice.InvoiceNumber}";

            if (Enum.IsDefined(typeof(PaymentProvider), invoice.PaymentProvider)
                && invoice.PaymentProvider != 0)
            {
                provider = invoice.PaymentProvider;
            }
        }

        IPaymentProcessor processor;
        try
        {
            processor = provider == PaymentProvider.Square
                ? await GetSquareProcessorForCheckoutAsync(orgId, provider)
                : _processorFactory.GetProcessor(provider);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        string checkoutUrl;
        if (request.Mode == "subscription")
        {
            try
            {
                checkoutUrl = await processor.CreateSubscriptionCheckoutSessionAsync(request);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        else
        {
            if (provider == PaymentProvider.Stripe)
            {
                request.ConnectedAccountId = organization.StripeConnectAccountId;
            }
            PaymentSessionResult paymentIntent;
            try
            {
                paymentIntent = await processor.CreatePaymentIntentAsync(request);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(new
            {
                clientSecret = paymentIntent.ClientSecret,
                url = paymentIntent.RedirectUrl,
                providerPaymentId = paymentIntent.ProviderPaymentId
            });
        }
        return Ok(new { url = checkoutUrl });
    }

    private async Task<IPaymentProcessor> GetSquareProcessorForCheckoutAsync(Guid orgId, PaymentProvider provider)
    {
        try
        {
            return await _processorFactory.GetProcessorForOrgAsync(orgId, provider);
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException("Your Square connection has expired. Disconnect and reconnect Square, then try checkout again.");
        }
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

        if (string.IsNullOrWhiteSpace(_stripeSettings.ApiKey)
            || string.IsNullOrWhiteSpace(_stripeSettings.ReturnUrl))
        {
            _logger.LogError(
                "Stripe connected account onboarding is not configured for org {OrganizationId}. ApiKeyConfigured={ApiKeyConfigured}, ReturnUrlConfigured={ReturnUrlConfigured}",
                orgId,
                !string.IsNullOrWhiteSpace(_stripeSettings.ApiKey),
                !string.IsNullOrWhiteSpace(_stripeSettings.ReturnUrl));

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Stripe onboarding is not configured for this environment.",
                code = "STRIPE_NOT_CONFIGURED",
                details = string.Empty
            });
        }

        var processor = _processorFactory.GetProcessor(organization.PaymentProvider.ToString());

        if (processor is IConnectedAccountProcessor connected)
        {
            try
            {
                var accountId = await connected.CreateConnectedAccountAsync();
                if (string.IsNullOrWhiteSpace(accountId))
                    return NotFound("Unable to create connected account.");

                organization.StripeConnectAccountId = accountId;
                organization.IsStripeConnected = true;

                var updatedOrg = await _organizationService.UpsertOrganization(organization);
                if (updatedOrg.IsFailure)
                    return BadRequest(updatedOrg.Error);

                var stripeReturnUrl = BuildStripeUiReturnUrl(accountId);
                var stripeRefreshUrl = BuildStripeUiRefreshUrl(accountId);
                var onboardingUrl = await connected.GenerateAccountLinkAsync(accountId, stripeReturnUrl, stripeRefreshUrl);
                return Ok(new { onboarding = onboardingUrl });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe connected account creation failed for org {OrganizationId}.", orgId);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "Stripe onboarding is currently unavailable. Please try again shortly.",
                    code = "STRIPE_UNAVAILABLE",
                    details = _hostEnvironment.IsDevelopment() ? ex.Message : string.Empty
                });
            }
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
            if (updated.IsSuccess)
                await _onboardingService.MarkStepCompleteAsync(orgId, OnboardingStepKeys.ConnectStripe);
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
        if (orgUpdate.IsSuccess)
            await _onboardingService.MarkStepCompleteAsync(orgId, OnboardingStepKeys.ConnectStripe);

        return orgUpdate.IsSuccess ? Ok(new { linked = true }) : BadRequest(orgUpdate.Error);
    }

    [HttpDelete("square/disconnect")]
    public async Task<IActionResult> DisconnectSquare()
    {
        var orgId = HttpContext.GetOrganizationId();
        var orgResult = await _organizationService.GetOrganiztionById(orgId);
        if (orgResult.IsFailure)
            return NotFound(orgResult.Error);

        var organization = orgResult.Value;
        organization.IsSquareConnected = false;
        organization.SquareMerchantId = null;

        var updateResult = await _organizationService.UpsertOrganization(organization);
        if (updateResult.IsFailure)
            return BadRequest(updateResult.Error);

        var profileDisconnectResult = await _paymentProfileService.DisconnectOrganizationProviderAsync(orgId, PaymentProvider.Square);
        if (profileDisconnectResult.IsFailure)
            return BadRequest(profileDisconnectResult.Error);

        return Ok(new { disconnected = true });
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

        if (!request.InvoiceId.HasValue || request.InvoiceId.Value == Guid.Empty)
            return BadRequest("Invoice id is required for refunds.");

        var invoiceResult = await _invoiceService.GetInvoiceByIdAsync(request.InvoiceId.Value);
        if (!invoiceResult.IsSuccess)
            return NotFound("Invoice not found.");

        var invoice = invoiceResult.Value;
        if (invoice.OrganizationId != orgId)
            return Unauthorized();

        if (invoice.AmountPaid <= 0)
            return BadRequest("Invoice has no paid amount available to refund.");

        if (request.Amount <= 0)
            return BadRequest("Refund amount must be greater than zero.");

        if (request.Amount > invoice.AmountPaid)
            return BadRequest($"Refund amount cannot exceed paid amount ({invoice.AmountPaid:0.##}).");

        var expectedProviderPaymentId = invoice.ExternalPaymentId;

        if (!string.IsNullOrWhiteSpace(expectedProviderPaymentId)
            && !string.Equals(request.ProviderPaymentId?.Trim(), expectedProviderPaymentId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Provider payment id does not match the selected invoice.");
        }

        var processor = request.Provider == PaymentProvider.Square
            ? await _processorFactory.GetProcessorForOrgAsync(orgId, request.Provider)
            : _processorFactory.GetProcessor(request.Provider);
        if (processor is not IPaymentOperationsProcessor ops)
            return BadRequest("Processor does not support refund operations.");

        var result = await ops.RefundPaymentAsync(new PaymentRefundRequest
        {
            InvoiceId = request.InvoiceId,
            ProviderPaymentId = request.ProviderPaymentId?.Trim() ?? string.Empty,
            Amount = request.Amount,
            Currency = request.Currency,
            Reason = request.Reason,
            ConnectedAccountId = request.Provider == PaymentProvider.Stripe
                ? orgResult.Value.StripeConnectAccountId
                : null
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

        var processor = request.Provider == PaymentProvider.Square
            ? await _processorFactory.GetProcessorForOrgAsync(orgId, request.Provider)
            : _processorFactory.GetProcessor(request.Provider);
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
            ConnectedAccountId = request.Provider == PaymentProvider.Stripe
                ? orgResult.Value.StripeConnectAccountId
                : null
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

        var processor = request.Provider == PaymentProvider.Square
            ? await _processorFactory.GetProcessorForOrgAsync(orgId, request.Provider)
            : _processorFactory.GetProcessor(request.Provider);
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
            ConnectedAccountId = request.Provider == PaymentProvider.Stripe
                ? orgResult.Value.StripeConnectAccountId
                : null
        });

        // Record the deposit against the linked invoice
        if (request.InvoiceId.HasValue && !string.IsNullOrEmpty(result.ProviderPaymentId))
        {
            await _invoiceService.RecordDepositAsync(
                request.InvoiceId.Value,
                request.Amount,
                request.Provider,
                result.ProviderPaymentId);
        }

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
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        if (request.PaymentProfileId == Guid.Empty)
            return BadRequest("Payment profile is required.");

        if (string.IsNullOrWhiteSpace(request.ProviderSubscriptionId))
            return BadRequest("Provider subscription ID is required.");

        var normalizedStatus = NormalizeSubscriptionStatus(request.Status);
        if (normalizedStatus is null)
            return BadRequest("Invalid subscription status.");

        var result = await _subscriptionRecordService.CreateAsync(
            request.PaymentProfileId,
            request.ProviderSubscriptionId,
            request.ProviderPriceId,
            normalizedStatus,
            ""
        );

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // POST: api/payments/subscription/cancel
    [HttpPost("subscription/cancel")]
    [Authorize(Policy = "OrganizationAdminOnly")]
    public async Task<IActionResult> CancelSubscription([FromBody] CancelSubscriptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderSubscriptionId))
            return BadRequest("Provider subscription ID is required.");

        var subscriptionResult = await _subscriptionRecordService.GetByProviderIdAsync(request.ProviderSubscriptionId);
        if (subscriptionResult.IsFailure)
            return NotFound(subscriptionResult.Error);

        if (string.Equals(subscriptionResult.Value.Status, "canceled", StringComparison.OrdinalIgnoreCase))
            return Ok();

        var orgId = HttpContext.GetOrganizationId();
        var orgResult = await _organizationService.GetOrganiztionById(orgId);
        if (orgResult.IsFailure)
            return NotFound(orgResult.Error);

        var processor = orgResult.Value.PaymentProvider == PaymentProvider.Square
            ? await _processorFactory.GetProcessorForOrgAsync(orgId, orgResult.Value.PaymentProvider)
            : _processorFactory.GetProcessor(orgResult.Value.PaymentProvider);

        if (processor is not ISubscriptionOperationsProcessor subOps)
            return BadRequest("Processor does not support subscription cancellation.");

        var providerResult = await subOps.CancelSubscriptionAsync(request.ProviderSubscriptionId);
        if (!providerResult.Success)
            return BadRequest(providerResult);

        var canceledAt = providerResult.SubscriptionExpiresAtUtc
                 ?? (request.CanceledAt == default ? DateTime.UtcNow : request.CanceledAt.ToUniversalTime());

        var result = await _subscriptionRecordService.CancelAsync(
            request.ProviderSubscriptionId,
            canceledAt
        );

        if (result.IsSuccess)
            await _organizationService.UpdateSubscriptionStateAsync(
                orgId,
                providerResult.SubscriptionStatus ?? "canceled",
                providerResult.SubscriptionPlanName,
                canceledAt);

        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPost("subscription/change-plan")]
    [Authorize(Policy = "OrganizationAdminOnly")]
    [EnableRateLimiting("payment-sensitive")]
    public async Task<IActionResult> ChangeSubscriptionPlan([FromBody] ChangeSubscriptionPlanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderSubscriptionId))
            return BadRequest("Provider subscription ID is required.");

        if (string.IsNullOrWhiteSpace(request.ProviderPriceId))
            return BadRequest("Provider price ID is required.");

        var orgId = HttpContext.GetOrganizationId();
        var orgResult = await _organizationService.GetOrganiztionById(orgId);
        if (orgResult.IsFailure)
            return NotFound(orgResult.Error);

        var processor = orgResult.Value.PaymentProvider == PaymentProvider.Square
            ? await _processorFactory.GetProcessorForOrgAsync(orgId, orgResult.Value.PaymentProvider)
            : _processorFactory.GetProcessor(orgResult.Value.PaymentProvider);

        if (processor is not ISubscriptionOperationsProcessor subOps)
            return BadRequest("Processor does not support subscription plan changes.");

        var providerResult = await subOps.ChangeSubscriptionPlanAsync(
            request.ProviderSubscriptionId,
            request.ProviderPriceId);

        if (!providerResult.Success)
            return BadRequest(providerResult);

        var subscriptionResult = await _subscriptionRecordService.GetByProviderIdAsync(request.ProviderSubscriptionId);
        if (subscriptionResult.IsSuccess)
        {
            subscriptionResult.Value.ProviderPriceId = request.ProviderPriceId.Trim();
            if (!string.IsNullOrWhiteSpace(providerResult.SubscriptionPlanName))
                subscriptionResult.Value.PlanName = providerResult.SubscriptionPlanName;

            if (!string.IsNullOrWhiteSpace(providerResult.SubscriptionStatus))
                subscriptionResult.Value.Status = providerResult.SubscriptionStatus;

            await _subscriptionRecordService.UpdateAsync(subscriptionResult.Value);
            await _organizationService.UpdateSubscriptionStateAsync(
                orgId,
                subscriptionResult.Value.Status,
                subscriptionResult.Value.PlanName,
                providerResult.SubscriptionExpiresAtUtc);
        }

        return Ok(providerResult);
    }

    [HttpGet("history")]
    [Authorize(Policy = "OrganizationAdminOnly")]
    public async Task<IActionResult> GetPaymentHistory(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 100)
    {
        var orgId = HttpContext.GetOrganizationId();
        var result = await _paymentHistoryService.GetPaymentEventsForEntityAsync(
            orgId,
            fromUtc,
            toUtc,
            Math.Clamp(pageSize, 1, MaxPageSize),
            cursor,
            disputesOnly: false);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("disputes")]
    [Authorize(Policy = "OrganizationAdminOnly")]
    public async Task<IActionResult> GetDisputes(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 100)
    {
        var orgId = HttpContext.GetOrganizationId();
        var result = await _paymentHistoryService.GetPaymentEventsForEntityAsync(
            orgId,
            fromUtc,
            toUtc,
            Math.Clamp(pageSize, 1, MaxPageSize),
            cursor,
            disputesOnly: true);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("financial-summary")]
    [Authorize(Policy = "OrganizationAdminOnly")]
    public async Task<IActionResult> GetFinancialSummary()
    {
        var orgId = HttpContext.GetOrganizationId();
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var cacheKey = $"payments:financial-summary:{orgId}:{now:yyyyMM}";

        var cached = await _distributedCache.GetStringAsync(cacheKey, HttpContext.RequestAborted);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedSummary = JsonSerializer.Deserialize<FinancialSummaryDto>(cached);
            if (cachedSummary is not null)
                return Ok(cachedSummary);
        }

        var historyResult = await _paymentHistoryService.GetFinancialAggregatesAsync(orgId, monthStart);
        if (historyResult.IsFailure)
            return BadRequest(historyResult.Error);

        var invoicesResult = await _invoiceService.GetInvoiceAggregatesByOrganizationAsync(orgId);
        if (invoicesResult.IsFailure)
            return BadRequest(invoicesResult.Error);

        var summary = new FinancialSummaryDto
        {
            GrossCollected = historyResult.Value.GrossCollectedMinor / 100m,
            Refunded = historyResult.Value.RefundedMinorAbsolute / 100m,
            NetCollected = (historyResult.Value.GrossCollectedMinor - historyResult.Value.RefundedMinorAbsolute) / 100m,
            MonthCollected = historyResult.Value.MonthCollectedMinor / 100m,
            Outstanding = invoicesResult.Value.Outstanding,
            DisputeCount = historyResult.Value.DisputeCount,
            InvoiceCount = invoicesResult.Value.InvoiceCount
        };

        await _distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(summary),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = FinancialSummaryCacheTtl
            },
            HttpContext.RequestAborted);

        return Ok(summary);
    }

    [HttpGet("subscription/current")]
    [Authorize(Policy = "OrganizationAdminOnly")]
    public async Task<IActionResult> GetCurrentSubscription()
    {
        var orgId = HttpContext.GetOrganizationId();
        var orgResult = await _organizationService.GetOrganiztionById(orgId);
        if (orgResult.IsFailure)
            return NotFound(orgResult.Error);

        var subscriptionResult = await _subscriptionRecordService.GetLatestForOrganizationAsync(orgId, orgResult.Value.PaymentProvider);
        if (subscriptionResult.IsFailure)
            return NotFound(subscriptionResult.Error);

        return Ok(subscriptionResult.Value);
    }

    [HttpGet("subscription/plans")]
    [Authorize(Policy = "OrganizationAdminOnly")]
    public async Task<IActionResult> GetSubscriptionPlans()
    {
        var configuredPlans = new[]
        {
            new { PlanKey = "go", Cycle = "monthly", PriceId = _stripeSettings.GoMonthlyPrice },
            new { PlanKey = "go", Cycle = "yearly", PriceId = _stripeSettings.GoYearlyPrice },
            new { PlanKey = "flow", Cycle = "monthly", PriceId = _stripeSettings.FlowMonthlyPrice },
            new { PlanKey = "flow", Cycle = "yearly", PriceId = _stripeSettings.FlowYearlyPrice },
            new { PlanKey = "max", Cycle = "monthly", PriceId = _stripeSettings.MaxMonthlyPrice },
            new { PlanKey = "max", Cycle = "yearly", PriceId = _stripeSettings.MaxYearlyPrice }
        }
        .Where(x => !string.IsNullOrWhiteSpace(x.PriceId))
        .ToList();

        if (configuredPlans.Count == 0 || string.IsNullOrWhiteSpace(_stripeSettings.ApiKey))
            return Ok(Array.Empty<SubscriptionPlanPriceDto>());

        try
        {
            var priceService = new PriceService();
            var tasks = configuredPlans.Select(async configuredPlan =>
            {
                var stripePrice = await priceService.GetAsync(configuredPlan.PriceId.Trim());
                var amount = stripePrice.UnitAmount.HasValue
                    ? stripePrice.UnitAmount.Value / 100m
                    : 0m;

                return new SubscriptionPlanPriceDto
                {
                    PlanKey = configuredPlan.PlanKey,
                    Cycle = configuredPlan.Cycle,
                    ProviderPriceId = configuredPlan.PriceId.Trim(),
                    Amount = amount,
                    Currency = string.IsNullOrWhiteSpace(stripePrice.Currency) ? "usd" : stripePrice.Currency
                };
            });

            var prices = await Task.WhenAll(tasks);
            return Ok(prices);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to retrieve live Stripe subscription plan pricing.");
            return Ok(Array.Empty<SubscriptionPlanPriceDto>());
        }
    }

    private static string? NormalizeSubscriptionStatus(string? status)
    {
        var normalized = string.IsNullOrWhiteSpace(status)
            ? "active"
            : status.Trim().ToLowerInvariant();

        return normalized switch
        {
            "active" => "active",
            "trialing" => "trialing",
            "past_due" => "past_due",
            "incomplete" => "incomplete",
            "incomplete_expired" => "incomplete_expired",
            "unpaid" => "unpaid",
            "paused" => "paused",
            "canceled" => "canceled",
            _ => null
        };
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
    public async Task<IActionResult> HandleStripeWebhook()
    {
        if (string.IsNullOrWhiteSpace(_stripeSettings.WebhookKey))
        {
            _logger.LogError("Stripe webhook key is not configured. Path={Path}", HttpContext.Request.Path);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

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
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature validation failed. Path={Path}", HttpContext.Request.Path);
            return BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected Stripe webhook error. Path={Path}", HttpContext.Request.Path);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        await _stripeWebhookService.HandleEventAsync(stripeEvent);
        await MarkProcessedAsync(GetStripeReplayCacheKey(stripeEvent.Id), WebhookReplayWindow, HttpContext.RequestAborted);

        return Ok();
    }

    [HttpPost("square/webhook")]
    [AllowAnonymous]
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

        var connectBaseUrl = _squareSettings.UseSandbox
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
        var root = tokenDocument.RootElement;

        var merchantId = root.TryGetProperty("merchant_id", out var merchantElement)
            ? merchantElement.GetString()
            : null;
        var accessToken = root.TryGetProperty("access_token", out var atEl)
            ? atEl.GetString()
            : null;
        var refreshToken = root.TryGetProperty("refresh_token", out var rtEl)
            ? rtEl.GetString()
            : null;
        var expiresAt = root.TryGetProperty("expires_at", out var expEl)
            ? expEl.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(merchantId))
            return Redirect($"{uiBase}?provider=square&success=false&error={Uri.EscapeDataString("Square merchant id was not returned.")}");

        if (string.IsNullOrWhiteSpace(accessToken))
            return Redirect($"{uiBase}?provider=square&success=false&error={Uri.EscapeDataString("Square access token was not returned.")}");

        var orgResult = await _organizationService.GetOrganiztionById(organizationId);
        if (orgResult.IsFailure)
            return Redirect($"{uiBase}?provider=square&success=false&error={Uri.EscapeDataString("Organization not found.")}");

        var organization = orgResult.Value;
        organization.PaymentProvider = PaymentProvider.Square;
        organization.SquareMerchantId = merchantId;
        organization.IsSquareConnected = true;

        var tokenExpiresAtUtc = !string.IsNullOrWhiteSpace(expiresAt)
            ? DateTime.Parse(expiresAt).ToUniversalTime()
            : DateTime.UtcNow.AddDays(30);

        var profileResult = await _paymentProfileService.UpsertWithTokensAsync(
            organizationId,
            PaymentEntityType.Organization,
            PaymentProvider.Square,
            merchantId,
            _squareTokenEncryption.Encrypt(accessToken),
            string.IsNullOrWhiteSpace(refreshToken) ? string.Empty : _squareTokenEncryption.Encrypt(refreshToken),
            tokenExpiresAtUtc,
            null);

        if (profileResult.IsFailure)
            return Redirect($"{uiBase}?provider=square&success=false&error={Uri.EscapeDataString(profileResult.Error?.ToString() ?? "Unable to save Square payment profile.")}");

        var updateResult = await _organizationService.UpsertOrganization(organization);
        if (updateResult.IsFailure)
            return Redirect($"{uiBase}?provider=square&success=false&error={Uri.EscapeDataString(updateResult.Error?.ToString() ?? "Unable to update organization payment provider.")}");

        await _onboardingService.MarkStepCompleteAsync(organizationId, OnboardingStepKeys.ConnectStripe);

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

        var connectBaseUrl = _squareSettings.UseSandbox
            ? "https://connect.squareupsandbox.com"
            : "https://connect.squareup.com";

        return $"{connectBaseUrl}/oauth2/authorize?client_id={Uri.EscapeDataString(_squareSettings.ApplicationId)}&response_type=code&scope=PAYMENTS_WRITE+PAYMENTS_READ+ORDERS_READ+ORDERS_WRITE+SUBSCRIPTIONS_READ+SUBSCRIPTIONS_WRITE&state={Uri.EscapeDataString(protectedState)}&redirect_uri={Uri.EscapeDataString(_squareSettings.RedirectUrl)}";
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

    private string BuildStripeUiCallbackBaseUrl()
    {
        var baseUrl = (_frontEndSettings.BaseUrl ?? "http://localhost:4200").TrimEnd('/');
        return $"{baseUrl}/admin/connectedpayment";
    }

    private string BuildStripeUiReturnUrl(string accountId)
    {
        var callbackUrl = BuildStripeUiCallbackBaseUrl();
        return $"{callbackUrl}?provider=stripe&success=true&accountId={Uri.EscapeDataString(accountId)}";
    }

    private string BuildStripeUiRefreshUrl(string accountId)
    {
        var callbackUrl = BuildStripeUiCallbackBaseUrl();
        return $"{callbackUrl}?provider=stripe&success=false&accountId={Uri.EscapeDataString(accountId)}";
    }

    private string BuildSquareUiRedirectBaseUrl()
    {
        var baseUrl = (_frontEndSettings.BaseUrl ?? "http://localhost:4200").TrimEnd('/');
        return $"{baseUrl}/admin/connectedpayment";
    }
}