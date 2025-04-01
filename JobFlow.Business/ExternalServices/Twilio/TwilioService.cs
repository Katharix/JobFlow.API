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

namespace JobFlow.Business.ExternalServices.Twilio
{
    public class TwilioService : ITwilioService
    {
        private readonly ITwilioSettings twilioSettings;
        private readonly ILogger<TwilioService> logger;

        public TwilioService(ILogger<TwilioService> logger, ITwilioSettings twilioSettings)
        {
            this.logger = logger;
            this.twilioSettings = twilioSettings;
            TwilioClient.Init(twilioSettings.AccountSId, twilioSettings.AuthToken,twilioSettings.AccountSId);
        }
        public async Task SendTextMessage(TwilioModel model)
        {
            try
            {
                var fromPhoneNumber = this.twilioSettings.SenderPhoneNumber;
                await MessageResource.CreateAsync(
                    to: new PhoneNumber(model.RecipientPhoneNumber),
                    from: new PhoneNumber(fromPhoneNumber),
                    body: model.Message);
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
