using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;

namespace JobFlow.Infrastructure.ExternalServices.ConfigurationModels;

public class TwilioSettings : ITwilioSettings
{
    public string AccountSId { get; set; }
    public string AuthToken { get; set; }
    public string SenderPhoneNumber { get; set; }
    public string MessagingServiceSid { get; set; }
}