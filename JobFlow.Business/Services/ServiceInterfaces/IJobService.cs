using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IJobService
{
    Task<Result<Job>> GetJobByIdAsync(Guid id, Guid organizationId);
    Task<Result<IEnumerable<Job>>> GetJobsByDate(DateTime date);
    Task<Result<IEnumerable<Job>>> GetJobsByStatusAsync(Guid statusId, Guid organizationId);
    Task<Result<Job>> UpsertJobAsync(Job model, Guid organizationId);
    Task<Result> DeleteJobAsync(Guid id);
}