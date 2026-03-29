using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;

namespace JobFlow.Infrastructure.ExternalServices.ConfigurationModels;

public class TwilioSettings : ITwilioSettings
{
    public string AccountSId { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string SenderPhoneNumber { get; set; } = string.Empty;
    public string MessagingServiceSid { get; set; } = string.Empty;
}