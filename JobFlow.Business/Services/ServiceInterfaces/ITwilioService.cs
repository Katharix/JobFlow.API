using JobFlow.Business.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface ITwilioService
{
    Task SendTextMessage(TwilioModel model);
}