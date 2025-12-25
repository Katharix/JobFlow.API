namespace JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;

public interface IStripeSettings
{
    string ApiKey { get; set; }
    string ReturnUrl { get; set; }
    string RefreshUrl { get; set; }
    string WebhookKey { get; set; }
}