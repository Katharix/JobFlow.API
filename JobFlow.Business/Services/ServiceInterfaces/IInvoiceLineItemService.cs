using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IInvoiceLineItemService
    {
        Task<Result<IEnumerable<InvoiceLineItem>>> GetByInvoiceIdAsync(Guid invoiceId);
        Task<Result> DeleteByInvoiceIdAsync(Guid invoiceId);
    }

}
