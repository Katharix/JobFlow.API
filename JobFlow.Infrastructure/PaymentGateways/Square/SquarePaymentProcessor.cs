using JobFlow.Business.DI;
using JobFlow.Business.PaymentGateways;
using JobFlow.Business.PaymentGateways.SharedModels;
using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;
using System.Net.Http.Json;
using Square;
using Square.Checkout.PaymentLinks;
using Microsoft.Extensions.Hosting;


namespace JobFlow.Infrastructure.PaymentGateways.SquarePayment;

[ScopedService]
public class SquarePaymentProcessor : IPaymentProcessor, IPaymentOperationsProcessor, ISubscriptionOperationsProcessor
{
    private readonly ISquareSettings _settings;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly string _baseUrl;

    // Per-org token override (set by the factory when resolving for a specific org)
    private string? _orgAccessToken;
    private string? _orgLocationId;

    public SquarePaymentProcessor(ISquareSettings settings, IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
        _hostEnvironment = hostEnvironment;

        _baseUrl = hostEnvironment.IsDevelopment()
            ? "https://connect.squareupsandbox.com"
            : "https://connect.squareup.com";
    }

    /// <summary>
    /// Configure this processor instance to use a specific org's OAuth token and location.
    /// </summary>
    public void ConfigureForOrganization(string accessToken, string? locationId)
    {
        _orgAccessToken = accessToken;
        _orgLocationId = locationId;
    }

    private string ResolveAccessToken()
    {
        var token = _orgAccessToken ?? _settings.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Square access token is not configured.");
        return token;
    }

    private string ResolveLocationId()
    {
        var locationId = _orgLocationId ?? _settings.LocationId;
        if (string.IsNullOrWhiteSpace(locationId))
            throw new InvalidOperationException("Square location id is not configured.");
        return locationId;
    }

    private SquareClient CreateSquareClient()
    {
        return new SquareClient(ResolveAccessToken(), new ClientOptions
        {
            BaseUrl = _baseUrl
        });
    }

    public async Task<string> CreateCheckoutSessionAsync(PaymentSessionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ProductName))
            throw new InvalidOperationException("Product name is required.");

        var amountDecimal = request.Amount ?? throw new InvalidOperationException("Payment amount is required.");
        var amount = (long)decimal.Round(amountDecimal * 100m, 0, MidpointRounding.AwayFromZero); // dollars -> cents
        var idempotencyKey = Guid.NewGuid().ToString();

        var money = new Money
        {
            Amount = amount,
            Currency = Currency.Usd
        };

        var quickPay = new QuickPay
        {
            Name = request.ProductName,
            PriceMoney = money,
            LocationId = ResolveLocationId()
        };

        var paymentLinkRequest = new CreatePaymentLinkRequest
        {
            QuickPay = quickPay,
            IdempotencyKey = idempotencyKey,
            Description = request.ProductName
        };

        if (request.InvoiceId.HasValue)
        {
            paymentLinkRequest.PaymentNote = $"invoiceId={request.InvoiceId.Value}";
        }

        try
        {
            var client = CreateSquareClient();
            var result = await client.Checkout.PaymentLinks.CreateAsync(paymentLinkRequest);
            return result.PaymentLink?.Url ?? throw new Exception("Square returned an empty payment link.");
        }
        catch (SquareApiException ex)
        {
            if (ex.Message.Contains("INSUFFICIENT_SCOPES", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("ORDERS_WRITE", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Square account needs updated permissions (ORDERS_WRITE). Please reconnect Square from Connected Payment to re-authorize the required scopes.",
                    ex);
            }

            throw new Exception($"Square API error: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new Exception("An error occurred while creating the Square checkout session.", ex);
        }
    }

    public Task<PaymentSessionResult> CreatePaymentIntentAsync(PaymentSessionRequest request)
    {
        return CreateCheckoutIntentAsync(request);
    }

    public Task<string> CreateSubscriptionCheckoutSessionAsync(PaymentSessionRequest request)
    {
        return CreateCheckoutSessionAsync(request);
    }

    public Task<PaymentSessionResult> CreateDepositPaymentAsync(PaymentSessionRequest request)
    {
        request.Amount = request.DepositAmount ?? request.Amount;
        return CreateCheckoutIntentAsync(request);
    }

    public async Task<PaymentOperationResult> RefundPaymentAsync(PaymentRefundRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderPaymentId))
            throw new InvalidOperationException("Provider payment id is required.");

        var amount = (long)decimal.Round(request.Amount * 100m, 0, MidpointRounding.AwayFromZero);

        using var client = CreateApiClient();
        var payload = new
        {
            idempotency_key = Guid.NewGuid().ToString(),
            payment_id = request.ProviderPaymentId,
            amount_money = new { amount, currency = request.Currency.ToUpperInvariant() },
            reason = request.Reason
        };

        var response = await client.PostAsJsonAsync("v2/refunds", payload);
        var responseBody = await response.Content.ReadAsStringAsync();

        return new PaymentOperationResult
        {
            Success = response.IsSuccessStatusCode,
            ProviderPaymentId = request.ProviderPaymentId,
            Amount = request.Amount,
            Currency = request.Currency,
            Message = response.IsSuccessStatusCode ? "refunded" : responseBody
        };
    }

    public async Task<PaymentOperationResult> AdjustPaymentAsync(PaymentAdjustmentRequest request)
    {
        if (request.AdjustmentAmount < 0)
        {
            return await RefundPaymentAsync(new PaymentRefundRequest
            {
                ProviderPaymentId = request.ProviderPaymentId,
                Amount = decimal.Abs(request.AdjustmentAmount),
                Currency = request.Currency,
                Reason = request.Reason
            });
        }

        return new PaymentOperationResult
        {
            Success = false,
            Message = "Positive adjustments require a new charge or deposit payment link."
        };
    }

    public async Task<PaymentOperationResult> CancelSubscriptionAsync(string providerSubscriptionId)
    {
        if (string.IsNullOrWhiteSpace(providerSubscriptionId))
            throw new InvalidOperationException("Provider subscription id is required.");

        using var client = CreateApiClient();
        var response = await client.PostAsJsonAsync($"v2/subscriptions/{providerSubscriptionId.Trim()}/cancel", new { });
        var responseBody = await response.Content.ReadAsStringAsync();

        return new PaymentOperationResult
        {
            Success = response.IsSuccessStatusCode,
            ProviderPaymentId = providerSubscriptionId,
            Message = response.IsSuccessStatusCode ? "canceled" : responseBody
        };
    }

    public Task<PaymentOperationResult> ChangeSubscriptionPlanAsync(string providerSubscriptionId, string providerPriceId)
    {
        return Task.FromResult(new PaymentOperationResult
        {
            Success = false,
            ProviderPaymentId = providerSubscriptionId,
            Message = "Square subscription plan change is not yet supported by this endpoint."
        });
    }

    private async Task<PaymentSessionResult> CreateCheckoutIntentAsync(PaymentSessionRequest request)
    {
        var url = await CreateCheckoutSessionAsync(request);
        return new PaymentSessionResult
        {
            RedirectUrl = url,
            ProviderPaymentId = Guid.NewGuid().ToString("N")
        };
    }

    private HttpClient CreateApiClient()
    {
        var accessToken = ResolveAccessToken();
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri($"{_baseUrl}/")
        };
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        httpClient.DefaultRequestHeaders.Add("Square-Version", "2025-10-16");
        return httpClient;
    }
}