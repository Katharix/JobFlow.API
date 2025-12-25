namespace JobFlow.Business.Services.ServiceInterfaces;

internal interface IJobOrderService
{
    Task<Result> AddJobToOrderAsync(Guid jobId, Guid orderId);
    Task<Result> RemoveJobFromOrderAsync(Guid jobId, Guid orderId);
    Task<Result<List<Guid>>> GetJobIdsForOrderAsync(Guid orderId);
}