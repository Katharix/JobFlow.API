using JobFlow.Business.ExternalServices.Twilio.Models;
using JobFlow.Business.Models.ConfigurationInterfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Twilio.Credential;
using Twilio.TwiML.Messaging;
using Twilio.AuthStrategies;
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
            try
            {
                var fromPhoneNumber = this.twilioSettings.SenderPhoneNumber;
                var messageOptions = new CreateMessageOptions(new PhoneNumber(model.RecipientPhoneNumber));
                messageOptions.MessagingServiceSid = "MG9caa2be2ffef65685f8a6f4aeb1de725";
                messageOptions.Body = model.Message;

                var message =  await MessageResource.CreateAsync(messageOptions);
            }
            catch (Exception ex)
            {

                throw ex;
            }
         
        }  
    }
    public interface ITwilioService
    {
        Task SendTextMessage(TwilioModel model);
    }
}
