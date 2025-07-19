using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Models
{
    public class NewsletterSubscriptionRequest
    {
        public string Email { get; set; } = string.Empty;
        public int ListId { get; set; }
        public string CaptchaToken { get; set; }
    }
}
