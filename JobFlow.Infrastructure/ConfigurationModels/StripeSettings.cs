using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.ExternalServices.ConfigurationModels
{
    public class StripeSettings : IStripeSettings
    {
        public string ApiKey { get; set; }
        public string ReturnUrl { get; set; }
        public string RefreshUrl { get; set; }
        public string WebhookKey { get; set; }

    }
}
