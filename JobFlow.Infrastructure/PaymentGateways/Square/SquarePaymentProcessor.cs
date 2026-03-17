using JobFlow.Business.DI;
using JobFlow.Business.PaymentGateways;
using JobFlow.Business.PaymentGateways.SharedModels;
using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;
using System.Net.Http.Json;
using Square;

using Square.Checkout.PaymentLinks;


namespace JobFlow.Infrastructure.PaymentGateways.SquarePayment;

[ScopedService]
public class SquarePaymentProcessor : IPaymentProcessor, IPaymentOperationsProcessor
{
    private readonly SquareClient _client;
    private readonly string _accessToken;
    private readonly string _locationId;

    public SquarePaymentProcessor(ISquareSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(settings.AccessToken))
            throw new InvalidOperationException("Square access token is not configured.");

        _accessToken = settings.AccessToken;

        _client = new SquareClient(settings.AccessToken, new ClientOptions
        {
            BaseUrl = "https://connect.squareupsandbox.com"
        });

        _locationId = settings.LocationId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_locationId))
            throw new InvalidOperationException("Square location id is not configured.");
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
            LocationId = _locationId
        };

        var paymentLinkRequest = new CreatePaymentLinkRequest
        {
            QuickPay = quickPay,
            IdempotencyKey = idempotencyKey
        };

        try
        {
            var result = await _client.Checkout.PaymentLinks.CreateAsync(paymentLinkRequest);
            return result.PaymentLink?.Url ?? throw new Exception("Square returned an empty payment link.");
        }
        catch (SquareApiException ex)
        {
            throw new Exception($"Square API error: {ex.Message}", ex);
        }
        catch (Exception ex)
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
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://connect.squareupsandbox.com/")
        };
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        httpClient.DefaultRequestHeaders.Add("Square-Version", "2025-10-16");
        return httpClient;
    }
}