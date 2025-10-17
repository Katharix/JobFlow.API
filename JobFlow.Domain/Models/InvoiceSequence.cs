using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class InvoiceSequence : Entity
    {
        public Guid OrganizationId { get; set; }
        public int Year { get; set; }
        public int LastSequence { get; set; }
    }
}
