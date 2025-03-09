using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Models.ConfigurationInterfaces
{
    public interface IStripeSettings
    {
        string ApiKey { get; set; }
    }
}
