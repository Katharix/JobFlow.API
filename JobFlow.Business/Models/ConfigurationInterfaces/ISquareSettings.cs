using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Models.ConfigurationInterfaces
{
    public interface ISquareSettings
    {
        string? ApplicationId { get; set; }
        string? AccessToken { get; set; }
        string? LocationId { get; set; }
    }
}
