using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IEmployeeRolePresetService
{
    Task<Result<IEnumerable<EmployeeRolePreset>>> GetAvailablePresetsAsync(Guid organizationId, string? industryKey);
    Task<Result<EmployeeRolePreset>> GetByIdAsync(Guid organizationId, Guid presetId);
    Task<Result<EmployeeRolePreset>> CreateOrgPresetAsync(Guid organizationId, EmployeeRolePresetDto dto);
    Task<Result<EmployeeRolePreset>> UpdateOrgPresetAsync(Guid organizationId, Guid presetId, EmployeeRolePresetDto dto);
    Task<Result> DeleteOrgPresetAsync(Guid organizationId, Guid presetId);
    Task<Result<EmployeeRolePresetApplyResultDto>> ApplyPresetAsync(Guid organizationId, Guid presetId, bool overwriteExisting);
}
