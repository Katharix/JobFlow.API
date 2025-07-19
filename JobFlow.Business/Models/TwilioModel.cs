using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Models
{
    public class TwilioModel
    {
        public string? SenderPhoneNumber { get; set; }
        public string? RecipientPhoneNumber { get; set; }
        public string? Message { get; set; }
    }
}
