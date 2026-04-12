namespace JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;

public interface ISquareSettings
{
    string? ApplicationId { get; set; }
    string? ClientSecret { get; set; }
    string? AccessToken { get; set; }
    string? LocationId { get; set; }
    string? RedirectUrl { get; set; }
    string? WebhookSignatureKey { get; set; }
    string? WebhookNotificationUrl { get; set; }
    bool UseSandbox { get; set; }
}