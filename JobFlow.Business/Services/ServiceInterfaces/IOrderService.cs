using JobFlow.Domain.Models;

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
