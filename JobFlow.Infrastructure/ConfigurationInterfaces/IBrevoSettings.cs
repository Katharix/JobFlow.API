using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces
{
    public interface IBrevoSettings
    {
        string ApiKey { get; set; } 
    }
}
