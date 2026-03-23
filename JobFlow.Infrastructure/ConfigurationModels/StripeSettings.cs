using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;

namespace JobFlow.Infrastructure.ExternalServices.ConfigurationModels;

public class StripeSettings : IStripeSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string RefreshUrl { get; set; } = string.Empty;
    public string WebhookKey { get; set; } = string.Empty;
}