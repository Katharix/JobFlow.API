using Square;
using Square.Models;
using Square.Exceptions;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.DI;
using JobFlow.Business.PaymentGateways.SharedModels;
using JobFlow.Business.PaymentGateways;

namespace JobFlow.Business.Services.PaymentProcessors
{
    [ScopedService]
    public class SquarePaymentProcessor : IPaymentProcessor
    {
        private readonly SquareClient _client;
        private readonly string _locationId;

        public SquarePaymentProcessor(IConfiguration configuration)
        {
            _client = new SquareClient.Builder()
                .Environment(Square.Environment.Sandbox)
                .AccessToken(configuration["Square:AccessToken"])
                .Build();

            _locationId = configuration["Square:LocationId"];
        }

        public async Task<string> CreateCheckoutSessionAsync(PaymentSessionRequest request)
        {
            var amount = (long)(request.Amount * 100); // Convert dollars to cents
            var idempotencyKey = Guid.NewGuid().ToString();

            var quickPay = new QuickPay(
                name: request.ProductName,
                priceMoney: new Money(amount, "USD"),
                locationId: _locationId
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

    }
}
