using JobFlow.Business.Models.ConfigurationInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Models
{
    public class SquareSettings : ISquareSettings
    {
        public string? ApplicationId { get; set; }
        public string? AccessToken { get; set; }
        public string? LocationId { get; set; }
    }
}
