using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces
{
    public interface ITwilioSettings
    {
        string AccountSId { get; set; }
        string AuthToken { get; set; }
        string SenderPhoneNumber { get; set; }
        string MessagingServiceSid { get; set; }
    }
}
