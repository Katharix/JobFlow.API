using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JobFlow.Business.DI;
using JobFlow.Business.Onboarding;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Infrastructure.PaymentGateways.Square;

public interface ISquareWebhookService
{
    Task HandleEventAsync(string rawBody, string signatureHeader, string callbackUrl);
}

[ScopedService]
public class SquareWebhookService : ISquareWebhookService
{
    private readonly IInvoiceService _invoiceService;
    private readonly IPaymentProfileService _paymentProfileService;
    private readonly ISubscriptionRecordService _subscriptionRecordService;
    private readonly IOnboardingService _onboardingService;
    private readonly IPaymentHistoryService _paymentHistoryService;
    private readonly IOrganizationService _organizationService;
    private readonly IRepository<CustomerPaymentProfile> _paymentProfiles;
    private readonly ISquareSettings _squareSettings;
    private readonly ILogger<SquareWebhookService> _logger;

    public SquareWebhookService(
        IInvoiceService invoiceService,
        IPaymentProfileService paymentProfileService,
        ISubscriptionRecordService subscriptionRecordService,
        IOnboardingService onboardingService,
        IPaymentHistoryService paymentHistoryService,
        IOrganizationService organizationService,
        IUnitOfWork unitOfWork,
        ISquareSettings squareSettings,
        ILogger<SquareWebhookService> logger)
    {
        _invoiceService = invoiceService;
        _paymentProfileService = paymentProfileService;
        _subscriptionRecordService = subscriptionRecordService;
        _onboardingService = onboardingService;
        _paymentHistoryService = paymentHistoryService;
        _organizationService = organizationService;
        _paymentProfiles = unitOfWork.RepositoryOf<CustomerPaymentProfile>();
        _squareSettings = squareSettings;
        _logger = logger;
    }

    public async Task HandleEventAsync(string rawBody, string signatureHeader, string callbackUrl)
    {
        if (!IsValidSignature(rawBody, signatureHeader, callbackUrl))
            throw new InvalidOperationException("Invalid Square webhook signature.");

        using var document = JsonDocument.Parse(rawBody);
        var root = document.RootElement;

        var eventType = root.TryGetProperty("type", out var typeEl)
            ? typeEl.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(eventType))
            return;

        var merchantId = root.TryGetProperty("merchant_id", out var merchantEl)
            ? merchantEl.GetString()
            : null;

        _logger.LogInformation("Square webhook received: Type={Type}, MerchantId={MerchantId}", eventType, merchantId);

        Guid? organizationId = null;
        if (!string.IsNullOrWhiteSpace(merchantId))
        {
            var orgResult = await _organizationService.GetBySquareMerchantIdAsync(merchantId);
            if (orgResult.IsSuccess)
                organizationId = orgResult.Value.Id;
        }

        switch (eventType)
        {
            case "payment.created":
            case "payment.updated":
                await HandlePaymentEventAsync(root, rawBody, eventType, organizationId);
                break;

            case "refund.created":
            case "refund.updated":
                await HandleRefundEventAsync(root, rawBody, eventType, organizationId);
                break;

            case "subscription.created":
            case "subscription.updated":
                await HandleSubscriptionEventAsync(root, rawBody, eventType);
                break;

            case "oauth.authorization.revoked":
                await HandleOAuthRevokedAsync(merchantId);
                break;
        }
    }

    private async Task HandlePaymentEventAsync(JsonElement root, string rawBody, string eventType, Guid? organizationId)
    {
        var payment = root.GetProperty("data").GetProperty("object").GetProperty("payment");
        var status = payment.GetProperty("status").GetString();

        var paymentId = payment.GetProperty("id").GetString() ?? string.Empty;
        var note = payment.TryGetProperty("note", out var noteEl) ? noteEl.GetString() : null;
        var referenceId = payment.TryGetProperty("reference_id", out var refEl) ? refEl.GetString() : null;

        var amountCents = payment.GetProperty("amount_money").GetProperty("amount").GetInt64();
        var currency = payment.GetProperty("amount_money").GetProperty("currency").GetString() ?? "USD";

        Guid? invoiceId = TryExtractInvoiceId(referenceId) ?? TryExtractInvoiceId(note);
        if (status == "COMPLETED" && invoiceId.HasValue)
        {
            if (!await _invoiceService.IsPaidAsync(invoiceId.Value))
            {
                var paidResult = await _invoiceService.MarkPaidAsync(
                    invoiceId.Value,
                    PaymentProvider.Square,
                    paymentId,
                    amountCents / 100m);

                if (paidResult.IsSuccess)
                {
                    await _onboardingService.MarkStepCompleteAsync(
                        paidResult.Value.OrganizationClient.OrganizationId,
                        OnboardingStepKeys.ReceivePayment);
                }
            }
        }

        var entityId = organizationId ?? Guid.Empty;
        if (invoiceId.HasValue)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId.Value);
            if (invoice.IsSuccess)
            {
                entityId = invoice.Value.OrganizationId;
                await _paymentHistoryService.LogAsync(new PaymentHistory
                {
                    Id = Guid.NewGuid(),
                    PaymentProvider = PaymentProvider.Square,
                    EntityType = PaymentEntityType.Organization,
                    EntityId = entityId,
                    InvoiceId = invoice.Value.Id,
                    AmountPaid = amountCents,
                    Currency = currency,
                    Status = status ?? "UNKNOWN",
                    EventType = eventType,
                    PaidAt = DateTime.UtcNow,
                    RawEventJson = rawBody,
                    StripePaymentIntentId = paymentId
                });
            }
        }
        else
        {
            await _paymentHistoryService.LogAsync(new PaymentHistory
            {
                Id = Guid.NewGuid(),
                PaymentProvider = PaymentProvider.Square,
                EntityType = PaymentEntityType.Organization,
                EntityId = entityId,
                AmountPaid = amountCents,
                Currency = currency,
                Status = status ?? "UNKNOWN",
                EventType = eventType,
                PaidAt = DateTime.UtcNow,
                RawEventJson = rawBody,
                StripePaymentIntentId = paymentId
            });
        }
    }

    private async Task HandleSubscriptionEventAsync(JsonElement root, string rawBody, string eventType)
    {
        if (!TryGetNestedProperty(root, out var subscription, "data", "object", "subscription"))
            return;

        var providerSubscriptionId = TryGetString(subscription, "id");
        if (string.IsNullOrWhiteSpace(providerSubscriptionId))
            return;

        var providerCustomerId = TryGetString(subscription, "customer_id") ?? string.Empty;
        var providerPriceId = TryGetString(subscription, "plan_variation_id") ?? string.Empty;
        var planName = TryGetString(subscription, "plan_id") ?? string.Empty;
        var status = (TryGetString(subscription, "status") ?? eventType).ToLowerInvariant();

        var isCanceledStatus = status.Contains("cancel") || status.Contains("deactivated");
        if (isCanceledStatus)
        {
            await _subscriptionRecordService.CancelAsync(providerSubscriptionId, DateTime.UtcNow);
            await _paymentHistoryService.LogAsync(new PaymentHistory
            {
                Id = Guid.NewGuid(),
                PaymentProvider = PaymentProvider.Square,
                EntityType = PaymentEntityType.Organization,
                EntityId = Guid.Empty,
                AmountPaid = 0,
                Currency = "USD",
                Status = status,
                EventType = eventType,
                PaidAt = DateTime.UtcNow,
                RawEventJson = rawBody,
                SubscriptionId = providerSubscriptionId,
                CustomerId = providerCustomerId
            });
            return;
        }

        var existingResult = await _subscriptionRecordService.GetByProviderIdAsync(providerSubscriptionId);
        if (existingResult.IsSuccess)
        {
            existingResult.Value.Status = status;
            if (!string.IsNullOrWhiteSpace(providerPriceId))
                existingResult.Value.ProviderPriceId = providerPriceId;
            if (!string.IsNullOrWhiteSpace(planName))
                existingResult.Value.PlanName = planName;

            await _subscriptionRecordService.UpdateAsync(existingResult.Value);
            return;
        }

        if (string.IsNullOrWhiteSpace(providerCustomerId))
            return;

        var paymentProfile = await _paymentProfiles.Query()
            .FirstOrDefaultAsync(p =>
                p.Provider == PaymentProvider.Square &&
                p.ProviderCustomerId == providerCustomerId);

        if (paymentProfile == null)
        {
            var orgId = TryExtractOrganizationId(subscription);
            if (!orgId.HasValue)
                return;

            var upsertResult = await _paymentProfileService.UpsertAsync(
                orgId.Value,
                PaymentEntityType.Organization,
                PaymentProvider.Square,
                providerCustomerId
            );

            if (!upsertResult.IsSuccess)
                return;

            paymentProfile = upsertResult.Value;
        }

        await _subscriptionRecordService.CreateAsync(
            paymentProfile.Id,
            providerSubscriptionId,
            providerPriceId,
            status,
            planName
        );
    }

    private async Task HandleRefundEventAsync(JsonElement root, string rawBody, string eventType, Guid? organizationId)
    {
        var refund = root.GetProperty("data").GetProperty("object").GetProperty("refund");
        var status = refund.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : "UNKNOWN";
        var paymentId = refund.TryGetProperty("payment_id", out var paymentIdEl) ? paymentIdEl.GetString() : null;
        var amountCents = refund.GetProperty("amount_money").GetProperty("amount").GetInt64();
        var currency = refund.GetProperty("amount_money").GetProperty("currency").GetString() ?? "USD";

        await _paymentHistoryService.LogAsync(new PaymentHistory
        {
            Id = Guid.NewGuid(),
            PaymentProvider = PaymentProvider.Square,
            EntityType = PaymentEntityType.Organization,
            EntityId = organizationId ?? Guid.Empty,
            InvoiceId = null,
            AmountPaid = -amountCents,
            Currency = currency,
            Status = status ?? "UNKNOWN",
            EventType = eventType,
            PaidAt = DateTime.UtcNow,
            RawEventJson = rawBody,
            StripePaymentIntentId = paymentId
        });
    }

    private async Task HandleOAuthRevokedAsync(string? merchantId)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
        {
            _logger.LogWarning("Square oauth.authorization.revoked received without merchant_id");
            return;
        }

        _logger.LogInformation("Square OAuth revoked for MerchantId={MerchantId}", merchantId);
        await _organizationService.MarkSquareDisconnectedAsync(merchantId);
    }

    private bool IsValidSignature(string rawBody, string signatureHeader, string callbackUrl)
    {
        var signatureKey = _squareSettings.WebhookSignatureKey;
        if (string.IsNullOrWhiteSpace(signatureKey))
            return true;

        if (string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        var notificationUrl = _squareSettings.WebhookNotificationUrl;
        var signedPayload = $"{(string.IsNullOrWhiteSpace(notificationUrl) ? callbackUrl : notificationUrl)}{rawBody}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signatureKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var computed = Convert.ToBase64String(hash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signatureHeader));
    }

    private static Guid? TryExtractInvoiceId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();

        if (normalized.StartsWith("invoiceId=", StringComparison.OrdinalIgnoreCase))
            normalized = normalized["invoiceId=".Length..];

        return Guid.TryParse(normalized, out var id) ? id : null;
    }

    private static Guid? TryExtractOrganizationId(JsonElement subscription)
    {
        var referenceId = TryGetString(subscription, "reference_id");
        var organizationId = TryExtractInvoiceId(referenceId);
        if (organizationId.HasValue)
            return organizationId;

        if (TryGetProperty(subscription, out var metadata, "metadata") &&
            metadata.ValueKind == JsonValueKind.Object)
        {
            if (TryGetProperty(metadata, out var organizationValue, "organizationId") &&
                organizationValue.ValueKind == JsonValueKind.String &&
                Guid.TryParse(organizationValue.GetString(), out var orgId))
                return orgId;

            if (TryGetProperty(metadata, out organizationValue, "organization_id") &&
                organizationValue.ValueKind == JsonValueKind.String &&
                Guid.TryParse(organizationValue.GetString(), out orgId))
                return orgId;
        }

        return null;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return TryGetProperty(element, out var value, propertyName) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool TryGetNestedProperty(JsonElement element, out JsonElement result, params string[] path)
    {
        result = element;
        foreach (var segment in path)
        {
            if (!TryGetProperty(result, out result, segment))
                return false;
        }

        return true;
    }

    private static bool TryGetProperty(JsonElement element, out JsonElement result, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            result = default;
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                result = property.Value;
                return true;
            }
        }

        result = default;
        return false;
    }
}
