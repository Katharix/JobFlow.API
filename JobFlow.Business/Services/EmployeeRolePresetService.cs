using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class EmployeeRolePresetService : IEmployeeRolePresetService
{
    private readonly IRepository<EmployeeRolePreset> presets;
    private readonly IRepository<EmployeeRolePresetItem> presetItems;
    private readonly IRepository<EmployeeRole> roles;
    private readonly ILogger<EmployeeRolePresetService> logger;
    private readonly IUnitOfWork unitOfWork;

    public EmployeeRolePresetService(ILogger<EmployeeRolePresetService> logger, IUnitOfWork unitOfWork)
    {
        this.logger = logger;
        this.unitOfWork = unitOfWork;
        presets = unitOfWork.RepositoryOf<EmployeeRolePreset>();
        presetItems = unitOfWork.RepositoryOf<EmployeeRolePresetItem>();
        roles = unitOfWork.RepositoryOf<EmployeeRole>();
    }

    public async Task<Result<IEnumerable<EmployeeRolePreset>>> GetAvailablePresetsAsync(Guid organizationId, string? industryKey)
    {
        var query = presets.Query()
            .Include(p => p.Items)
            .Where(p => (p.OrganizationId == organizationId) || (p.IsSystem && p.IndustryKey == industryKey));

        var data = await query
            .OrderByDescending(p => p.IsSystem)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return Result.Success<IEnumerable<EmployeeRolePreset>>(data);
    }

    public async Task<Result<EmployeeRolePreset>> GetByIdAsync(Guid organizationId, Guid presetId)
    {
        var preset = await presets.Query()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == presetId);

        if (preset == null)
        {
            return Result.Failure<EmployeeRolePreset>(EmployeeRolePresetErrors.EmployeeRolePresetNotFound);
        }

        if (preset.OrganizationId.HasValue && preset.OrganizationId != organizationId)
        {
            return Result.Failure<EmployeeRolePreset>(EmployeeRolePresetErrors.EmployeeRolePresetForbidden);
        }

        return Result<EmployeeRolePreset>.Success(preset);
    }

    public async Task<Result<EmployeeRolePreset>> CreateOrgPresetAsync(Guid organizationId, EmployeeRolePresetDto dto)
    {
        var preset = new EmployeeRolePreset
        {
            OrganizationId = organizationId,
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            IndustryKey = string.IsNullOrWhiteSpace(dto.IndustryKey) ? null : dto.IndustryKey.Trim(),
            IsSystem = false
        };

        await presets.AddAsync(preset);
        await unitOfWork.SaveChangesAsync();

        await ReplacePresetItemsAsync(preset, dto.Items);

        return Result<EmployeeRolePreset>.Success(preset);
    }

    public async Task<Result<EmployeeRolePreset>> UpdateOrgPresetAsync(Guid organizationId, Guid presetId, EmployeeRolePresetDto dto)
    {
        var preset = await presets.Query().FirstOrDefaultAsync(p => p.Id == presetId);
        if (preset == null)
        {
            return Result.Failure<EmployeeRolePreset>(EmployeeRolePresetErrors.EmployeeRolePresetNotFound);
        }

        if (preset.IsSystem || preset.OrganizationId != organizationId)
        {
            return Result.Failure<EmployeeRolePreset>(EmployeeRolePresetErrors.EmployeeRolePresetForbidden);
        }

        preset.Name = dto.Name.Trim();
        preset.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        preset.IndustryKey = string.IsNullOrWhiteSpace(dto.IndustryKey) ? null : dto.IndustryKey.Trim();
        preset.UpdatedAt = DateTime.UtcNow;

        presets.Update(preset);
        await unitOfWork.SaveChangesAsync();

        await ReplacePresetItemsAsync(preset, dto.Items);

        return Result<EmployeeRolePreset>.Success(preset);
    }

    public async Task<Result> DeleteOrgPresetAsync(Guid organizationId, Guid presetId)
    {
        var preset = await presets.Query().FirstOrDefaultAsync(p => p.Id == presetId);
        if (preset == null)
        {
            return Result.Failure(EmployeeRolePresetErrors.EmployeeRolePresetNotFound);
        }

        if (preset.IsSystem || preset.OrganizationId != organizationId)
        {
            return Result.Failure(EmployeeRolePresetErrors.EmployeeRolePresetForbidden);
        }

        presets.Remove(preset);
        await unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<EmployeeRolePresetApplyResultDto>> ApplyPresetAsync(Guid organizationId, Guid presetId, bool overwriteExisting)
    {
        var presetResult = await GetByIdAsync(organizationId, presetId);
        if (presetResult.IsFailure)
        {
            return Result.Failure<EmployeeRolePresetApplyResultDto>(presetResult.Error);
        }

        var preset = presetResult.Value;
        var existingRoles = await roles.Query()
            .Where(r => r.OrganizationId == organizationId)
            .ToListAsync();

        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var item in preset.Items.OrderBy(i => i.SortOrder))
        {
            var normalizedName = item.Name.Trim();
            var match = existingRoles.FirstOrDefault(r => r.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                var newRole = new EmployeeRole
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = normalizedName.ToUpper(),
                    Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim()
                };
                await roles.AddAsync(newRole);
                existingRoles.Add(newRole);
                created += 1;
                continue;
            }

            if (!overwriteExisting)
            {
                skipped += 1;
                continue;
            }

            match.Description = string.IsNullOrWhiteSpace(item.Description)
                ? match.Description
                : item.Description.Trim();
            roles.Update(match);
            updated += 1;
        }

        await unitOfWork.SaveChangesAsync();

        return Result<EmployeeRolePresetApplyResultDto>.Success(new EmployeeRolePresetApplyResultDto
        {
            Created = created,
            Updated = updated,
            Skipped = skipped
        });
    }

    private async Task ReplacePresetItemsAsync(EmployeeRolePreset preset, IEnumerable<EmployeeRolePresetItemDto> items)
    {
        var existing = await presetItems.Query()
            .Where(item => item.PresetId == preset.Id)
            .ToListAsync();

        if (existing.Count > 0)
        {
            presetItems.RemoveRange(existing);
            await unitOfWork.SaveChangesAsync();
        }

        var list = items
            .Select((item, index) => new EmployeeRolePresetItem
            {
                PresetId = preset.Id,
                Name = item.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim(),
                SortOrder = item.SortOrder > 0 ? item.SortOrder : index + 1
            })
            .ToList();

        foreach (var item in list)
        {
            await presetItems.AddAsync(item);
        }

        await unitOfWork.SaveChangesAsync();
    }
}
