using JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces;

namespace JobFlow.Business.ConfigurationSettings;

public class PaymentSettings : IPaymentSettings
{
    public decimal ApplicationFee { get; }
}