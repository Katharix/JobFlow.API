using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IPaymentHistoryService
    {
        Task<Result> LogAsync(PaymentHistory history);
        Task<Result<List<PaymentHistory>>> GetPaymentsForEntityAsync(Guid entityId);
    }

}
