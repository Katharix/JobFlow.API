using JobFlow.Business.Models.ConfigurationInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Models.ConfigurationModels
{
    public class StripeSettings : IStripeSettings
    {
        public string ApiKey {get; set;}
    }
}
