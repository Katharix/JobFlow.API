using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;

namespace JobFlow.Infrastructure.ExternalServices.ConfigurationModels;

public class BrevoSettings : IBrevoSettings
{
    public string ApiKey { get; set; }
}