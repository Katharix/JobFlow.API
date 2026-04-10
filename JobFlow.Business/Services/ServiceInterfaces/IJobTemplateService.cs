using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IJobTemplateService
{
    Task<Result<IEnumerable<JobTemplate>>> GetAvailableTemplatesAsync(Guid organizationId, Guid? organizationTypeId);
    Task<Result<JobTemplate>> GetByIdAsync(Guid organizationId, Guid templateId);
    Task<Result<JobTemplate>> CreateOrgTemplateAsync(Guid organizationId, JobTemplateDto dto);
    Task<Result<JobTemplate>> UpdateOrgTemplateAsync(Guid organizationId, Guid templateId, JobTemplateDto dto);
    Task<Result> DeleteOrgTemplateAsync(Guid organizationId, Guid templateId);
}
