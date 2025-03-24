using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IOrderService
    {
        Task<Result<IEnumerable<Order>>> GetAllOrders();
        Task<Result<IEnumerable<Order>>> GetAllOrdersByOrganizationId(Guid organizationId);
        Task<Result<Order>> UpsertOrder(Order order);
        Task<Result<Order>> GetOrderById(Guid orderId);
        Task<Result> DeleteOrder(Guid orderId);
    }
}
