using JobFlow.Business.DI;
using JobFlow.Business.Models;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Infrastructure.Common;
using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
// for PollyPolicies
// for Policy.WrapAsync

namespace JobFlow.Infrastructure.ExternalServices.Twilio;

[ScopedService]
public class TwilioService : ITwilioService
{
    private readonly ILogger _logger;
    private readonly AsyncPolicy _policy;
    private readonly ITwilioSettings _settings;

    public TwilioService(
        ILogger<TwilioService> logger,
        ITwilioSettings twilioSettings)
    {
        _logger = logger;
        _settings = twilioSettings;

        // init Twilio
        TwilioClient.Init(_settings.AccountSId, _settings.AuthToken);

        // combine retry + circuit‐breaker
        _policy = Policy.WrapAsync(
            PollyPolicies.DefaultRetryPolicy(),
            PollyPolicies.DefaultCircuitBreakerPolicy()
        );
    }

    public async Task SendTextMessage(TwilioModel model)
    {
        var messageOptions = new CreateMessageOptions(
            new PhoneNumber(model.RecipientPhoneNumber))
        {
            MessagingServiceSid = _settings.MessagingServiceSid,
            Body = model.Message
        };

        // execute send under our resilience policies
        await _policy.ExecuteAsync(async () =>
        {
            var message = await MessageResource.CreateAsync(messageOptions);
            _logger.LogInformation(
                "Twilio message sent: SID={Sid}", message.Sid);
        });
    }
}