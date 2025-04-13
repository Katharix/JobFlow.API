using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Models.DTOs
{
    public class CancelSubscriptionRequest
    {
        public string ProviderSubscriptionId { get; set; } = string.Empty;
        public DateTime CanceledAt { get; set; } = DateTime.UtcNow;
    }

}
