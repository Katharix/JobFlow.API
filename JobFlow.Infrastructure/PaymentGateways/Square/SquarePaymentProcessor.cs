using JobFlow.Business.DI;
using JobFlow.Business.PaymentGateways;
using JobFlow.Business.PaymentGateways.SharedModels;
using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;
using Square;

using Square.Checkout.PaymentLinks;


namespace JobFlow.Infrastructure.PaymentGateways.SquarePayment;

[ScopedService]
public class SquarePaymentProcessor : IPaymentProcessor
{
    private readonly SquareClient _client;
    private readonly string _locationId;

    public SquarePaymentProcessor(ISquareSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(settings.AccessToken))
            throw new InvalidOperationException("Square access token is not configured.");

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
        => throw new NotImplementedException();

    public Task<string> CreateSubscriptionCheckoutSessionAsync(PaymentSessionRequest request)
        => throw new NotImplementedException();
}