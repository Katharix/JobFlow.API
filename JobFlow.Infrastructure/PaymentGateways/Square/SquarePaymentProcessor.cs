using JobFlow.Business.DI;
using JobFlow.Business.PaymentGateways;
using JobFlow.Business.PaymentGateways.SharedModels;
using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;
using Square;
using Square.Authentication;
using Square.Exceptions;
using Square.Models;
using Environment = Square.Environment;

namespace JobFlow.Infrastructure.PaymentGateways.SquarePayment;

[ScopedService]
public class SquarePaymentProcessor : IPaymentProcessor
{
    private readonly SquareClient _client;
    private readonly string _locationId;

    public SquarePaymentProcessor(ISquareSettings settings)
    {
        var bearerAuth = new BearerAuthModel.Builder(settings.AccessToken).Build();

        _client = new SquareClient.Builder()
            .Environment(Environment.Sandbox)
            .BearerAuthCredentials(bearerAuth)
            .Build();

        _locationId = settings.LocationId ?? "";
    }


    public async Task<string> CreateCheckoutSessionAsync(PaymentSessionRequest request)
    {
        var amount = (long)(request.Amount * 100); // Convert dollars to cents
        var idempotencyKey = Guid.NewGuid().ToString();

        var quickPay = new QuickPay(
            request.ProductName,
            new Money(amount, "USD"),
            _locationId
        );

        var paymentLinkRequest = new CreatePaymentLinkRequest(
            quickPay: quickPay,
            idempotencyKey: idempotencyKey
        );

        try
        {
            var result = await _client.CheckoutApi.CreatePaymentLinkAsync(paymentLinkRequest);
            return result.PaymentLink?.Url ?? throw new Exception("Square returned an empty payment link.");
        }
        catch (ApiException ex)
        {
            throw new Exception($"Square API error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception("An error occurred while creating the Square checkout session.", ex);
        }
    }

    public Task<string> CreateSubscriptionCheckoutSessionAsync(PaymentSessionRequest request)
    {
        throw new NotImplementedException();
    }
}