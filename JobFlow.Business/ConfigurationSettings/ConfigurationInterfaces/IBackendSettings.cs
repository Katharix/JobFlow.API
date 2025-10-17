using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces
{
    public interface IBackendSettings
    {
        string BaseUrl { get; }
    }
}
