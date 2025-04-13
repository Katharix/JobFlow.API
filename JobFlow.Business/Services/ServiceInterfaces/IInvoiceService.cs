using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IInvoiceService
    {
        Task<Result<Invoice>> GetInvoiceByIdAsync(Guid id);
        Task<Result<IEnumerable<Invoice>>> GetInvoicesByClientAsync(Guid clientId);
        Task<Result<Invoice>> UpsertInvoiceAsync(Invoice model);
        Task<Result> DeleteInvoiceAsync(Guid id);
    }
}
