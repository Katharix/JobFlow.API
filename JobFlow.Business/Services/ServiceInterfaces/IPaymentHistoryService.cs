using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IPaymentHistoryService
    {
        Task<Result> LogAsync(PaymentHistory history);
        Task<Result<List<PaymentHistory>>> GetPaymentsForEntityAsync(Guid entityId);
    }

}
