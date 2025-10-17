using JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.ConfigurationSettings
{
    public class BackendSettings : IBackendSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
    }

}
