using JobFlow.Domain.Models;
using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IEmployeeRoleService
{
    Task<Result<IEnumerable<EmployeeRole>>> GetRolesByOrganizationAsync(Guid organizationId);
    Task<Result<EmployeeRole>> GetByIdAsync(Guid id);
    Task<Result<IEnumerable<EmployeeRoleUsageDto>>> GetRoleUsageByOrganizationAsync(Guid organizationId);
    Task<Result<EmployeeRole>> UpsertAsync(EmployeeRole model);
    Task<Result> DeleteAsync(Guid id);
}