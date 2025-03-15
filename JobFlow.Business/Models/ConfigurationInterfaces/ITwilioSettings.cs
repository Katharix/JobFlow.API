using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Models.ConfigurationInterfaces
{
    public interface ITwilioSettings
    {
        string AccountSId { get; set; }
        string AuthToken { get; set; }
        string SenderPhoneNumber { get; set; }
    }
}
