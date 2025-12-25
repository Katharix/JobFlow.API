using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;

namespace JobFlow.Infrastructure.ExternalServices.ConfigurationModels;

public class ReCAPTCHASettings : IReCAPTCHASettings
{
    public string SecretKey { get; set; }
}