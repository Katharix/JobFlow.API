using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IJobService
    {
        Task<Result<Job>> GetJobByIdAsync(Guid id, Guid OrganizationId);
        Task<Result<IEnumerable<Job>>> GetJobsByDate(DateTime date);
        Task<Result<IEnumerable<Job>>> GetJobsByStatusAsync(Guid statusId, Guid OrganizationId);
        Task<Result<Job>> UpsertJobAsync(Job model);
        Task<Result> DeleteJobAsync(Guid id);
    }
}
