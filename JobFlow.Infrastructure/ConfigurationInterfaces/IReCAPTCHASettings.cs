namespace JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;

public interface IReCAPTCHASettings
{
    string SecretKey { get; set; }
}