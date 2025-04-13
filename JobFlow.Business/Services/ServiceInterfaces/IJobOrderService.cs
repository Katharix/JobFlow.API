using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    interface IJobOrderService
    {
        Task<Result> AddJobToOrderAsync(Guid jobId, Guid orderId);
        Task<Result> RemoveJobFromOrderAsync(Guid jobId, Guid orderId);
        Task<Result<List<Guid>>> GetJobIdsForOrderAsync(Guid orderId);
    }
}
