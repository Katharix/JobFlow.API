using JobFlow.Business.ExternalServices.Twilio.Models;
using JobFlow.Business.Models.ConfigurationInterfaces;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using JobFlow.Infrastructure.DI;

namespace JobFlow.Business.ExternalServices.Twilio
{
    [ScopedService]
    public class TwilioService : ITwilioService
    {
        private readonly ITwilioSettings twilioSettings;
        private readonly ILogger<TwilioService> logger;

        public TwilioService(ILogger<TwilioService> logger, ITwilioSettings twilioSettings)
        {
            this.logger = logger;
            this.twilioSettings = twilioSettings;
            TwilioClient.Init(twilioSettings.AccountSId, twilioSettings.AuthToken);
        }
        public async Task SendTextMessage(TwilioModel model)
        {
            var fromPhoneNumber = this.twilioSettings.SenderPhoneNumber;
            var messageOptions = new CreateMessageOptions(new PhoneNumber(model.RecipientPhoneNumber));
            messageOptions.MessagingServiceSid = this.twilioSettings.MessagingServiceSid;
            messageOptions.Body = model.Message;

            var message = await MessageResource.CreateAsync(messageOptions);
        }
    }
    public interface ITwilioService
    {
        Task SendTextMessage(TwilioModel model);
    }
}
