using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IJobService
{
    Task<Result<Job>> GetJobByIdAsync(Guid id, Guid organizationId);
    Task<Result<IEnumerable<Job>>> GetJobsByStatusAsync(Guid organizationId, JobLifecycleStatus status);
    Task<Result<Job>> UpsertJobAsync(Job model, Guid organizationId);
    Task<Result> DeleteJobAsync(Guid id);
    Task<Result<IEnumerable<JobDto>>> GetJobsAsync(Guid organizationId);
    Task<Result<Job>> UpdateJobStatusAsync(Guid organizationId, Guid jobId, JobLifecycleStatus status);
}