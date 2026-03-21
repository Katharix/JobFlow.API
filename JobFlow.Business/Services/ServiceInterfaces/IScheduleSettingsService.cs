using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IScheduleSettingsService
{
    Task<Result<ScheduleSettingsDto>> GetScheduleSettingsAsync(Guid organizationId);
    Task<Result<ScheduleSettingsDto>> UpsertScheduleSettingsAsync(Guid organizationId, ScheduleSettingsUpsertRequestDto dto);
}
