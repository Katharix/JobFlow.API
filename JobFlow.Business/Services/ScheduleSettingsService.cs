using JobFlow.Business;
using JobFlow.Business.DI;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Business.Services;

[ScopedService]
public class ScheduleSettingsService : IScheduleSettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<OrganizationScheduleSettings> _settings;

    public ScheduleSettingsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _settings = unitOfWork.RepositoryOf<OrganizationScheduleSettings>();
    }

    public async Task<Result<ScheduleSettingsDto>> GetScheduleSettingsAsync(Guid organizationId)
    {
        var settings = await _settings.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId);

        if (settings == null)
        {
            return Result.Success(new ScheduleSettingsDto
            {
                TravelBufferMinutes = 20,
                DefaultWindowMinutes = 120,
                EnforceTravelBuffer = true,
                AutoNotifyReschedule = true
            });
        }

        return Result.Success(Map(settings));
    }

    public async Task<Result<ScheduleSettingsDto>> UpsertScheduleSettingsAsync(
        Guid organizationId,
        ScheduleSettingsUpsertRequestDto dto)
    {
        if (dto.TravelBufferMinutes < 0 || dto.DefaultWindowMinutes < 0)
        {
            return Result.Failure<ScheduleSettingsDto>(
                Error.Validation("ScheduleSettings.InvalidValues", "Buffer and window values must be zero or greater."));
        }

        var settings = await _settings.Query()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId);

        if (settings == null)
        {
            settings = new OrganizationScheduleSettings
            {
                OrganizationId = organizationId
            };
            _settings.Add(settings);
        }

        settings.TravelBufferMinutes = dto.TravelBufferMinutes;
        settings.DefaultWindowMinutes = dto.DefaultWindowMinutes;
        settings.EnforceTravelBuffer = dto.EnforceTravelBuffer;
        settings.AutoNotifyReschedule = dto.AutoNotifyReschedule;

        await _unitOfWork.SaveChangesAsync();

        return Result.Success(Map(settings));
    }

    private static ScheduleSettingsDto Map(OrganizationScheduleSettings settings)
    {
        return new ScheduleSettingsDto
        {
            TravelBufferMinutes = settings.TravelBufferMinutes,
            DefaultWindowMinutes = settings.DefaultWindowMinutes,
            EnforceTravelBuffer = settings.EnforceTravelBuffer,
            AutoNotifyReschedule = settings.AutoNotifyReschedule
        };
    }
}
