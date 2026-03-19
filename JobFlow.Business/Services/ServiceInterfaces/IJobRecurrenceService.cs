using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IJobRecurrenceService
{
    Task<Result<JobRecurrence>> UpsertAsync(Guid jobId, Guid organizationId, JobRecurrenceUpsertRequest request);
}
