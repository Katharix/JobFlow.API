using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;

namespace JobFlow.Infrastructure.ExternalServices.ConfigurationModels;

public class SquareSettings : ISquareSettings
{
    public string? ApplicationId { get; set; }
    public string? ClientSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? LocationId { get; set; }
    public string? RedirectUrl { get; set; }
    public string? WebhookSignatureKey { get; set; }
    public string? WebhookNotificationUrl { get; set; }
    public bool UseSandbox { get; set; }
}