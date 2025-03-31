using JobFlow.Business.PaymentGateways.Stripe;
using JobFlow.Business.Services.PaymentProcessors;
using JobFlow.Domain.Enums;
using JobFlow.Infrastructure.DI;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.PaymentGateways
{
    [SingletonService]
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
    }

}
