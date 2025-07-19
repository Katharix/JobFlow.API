using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.ExternalServices.ConfigurationModels
{
    public class ReCAPTCHASettings : IReCAPTCHASettings
    {
        public string SecretKey { get; set; }
    }
}
