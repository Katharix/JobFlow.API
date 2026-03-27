using JobFlow.Business.DI;
using JobFlow.Business.PaymentGateways;
using JobFlow.Domain.Enums;
using JobFlow.Infrastructure.PaymentGateways.Square;
using JobFlow.Infrastructure.PaymentGateways.SquarePayment;
using JobFlow.Infrastructure.PaymentGateways.Stripe;
using Microsoft.Extensions.DependencyInjection;

namespace JobFlow.Infrastructure.PaymentGateways;

[ScopedService]
public class PaymentProcessorFactory : IPaymentProcessorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public PaymentProcessorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IPaymentProcessor GetProcessor(string provider)
    {
        return provider.ToLower() switch
        {
            "stripe" => _serviceProvider.GetRequiredService<StripePaymentProcessor>(),
            "square" => _serviceProvider.GetRequiredService<SquarePaymentProcessor>(),
            _ => throw new NotSupportedException($"Payment provider '{provider}' is not supported.")
        };
    }

    public IPaymentProcessor GetProcessor(PaymentProvider provider)
    {
        return GetProcessor(provider.ToString());
    }

    public async Task<IPaymentProcessor> GetProcessorForOrgAsync(Guid organizationId, PaymentProvider provider)
    {
        if (provider != PaymentProvider.Square)
            return GetProcessor(provider);

        var processor = _serviceProvider.GetRequiredService<SquarePaymentProcessor>();
        var refreshService = _serviceProvider.GetRequiredService<ISquareTokenRefreshService>();

        var tokenSet = await refreshService.RefreshIfNeededAsync(organizationId);
        if (tokenSet != null)
        {
            processor.ConfigureForOrganization(tokenSet.AccessToken, null);
        }

        return processor;
    }
}