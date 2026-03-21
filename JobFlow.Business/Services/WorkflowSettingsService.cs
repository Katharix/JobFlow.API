using JobFlow.Business;
using JobFlow.Business.DI;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Business.Services;

[ScopedService]
public class WorkflowSettingsService : IWorkflowSettingsService
{
    private const string JobLifecycleCategory = "JobLifecycle";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<OrganizationWorkflowStatus> _statuses;

    public WorkflowSettingsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _statuses = unitOfWork.RepositoryOf<OrganizationWorkflowStatus>();
    }

    public async Task<Result<List<WorkflowStatusDto>>> GetJobLifecycleStatusesAsync(Guid organizationId)
    {
        var stored = await _statuses.Query()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.Category == JobLifecycleCategory)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        if (stored.Count == 0)
        {
            var defaults = BuildDefaultJobLifecycleStatuses();
            return Result.Success(defaults);
        }

        var mapped = stored.Select(x => new WorkflowStatusDto
        {
            StatusKey = x.StatusKey,
            Label = x.Label,
            SortOrder = x.SortOrder
        }).ToList();

        return Result.Success(mapped);
    }

    public async Task<Result<List<WorkflowStatusDto>>> UpsertJobLifecycleStatusesAsync(
        Guid organizationId,
        List<WorkflowStatusUpsertRequestDto> statuses)
    {
        if (statuses == null || statuses.Count == 0)
        {
            return Result.Failure<List<WorkflowStatusDto>>(
                Error.Validation("Workflow.StatusesRequired", "At least one status is required."));
        }

        var normalized = statuses
            .Select(x => new WorkflowStatusUpsertRequestDto
            {
                StatusKey = x.StatusKey?.Trim() ?? string.Empty,
                Label = x.Label?.Trim() ?? string.Empty,
                SortOrder = x.SortOrder
            })
            .ToList();

        if (normalized.Any(x => string.IsNullOrWhiteSpace(x.StatusKey) || string.IsNullOrWhiteSpace(x.Label)))
        {
            return Result.Failure<List<WorkflowStatusDto>>(
                Error.Validation("Workflow.InvalidStatus", "Status key and label are required."));
        }

        var duplicateKeys = normalized
            .GroupBy(x => x.StatusKey, StringComparer.OrdinalIgnoreCase)
            .Any(g => g.Count() > 1);

        if (duplicateKeys)
        {
            return Result.Failure<List<WorkflowStatusDto>>(
                Error.Validation("Workflow.DuplicateStatus", "Status keys must be unique."));
        }

        foreach (var status in normalized)
        {
            if (!Enum.TryParse<JobLifecycleStatus>(status.StatusKey, true, out _))
            {
                return Result.Failure<List<WorkflowStatusDto>>(
                    Error.Validation("Workflow.InvalidStatusKey", $"Unknown status key: {status.StatusKey}."));
            }
        }

        var existing = await _statuses.Query()
            .Where(x => x.OrganizationId == organizationId && x.Category == JobLifecycleCategory)
            .ToListAsync();

        if (existing.Count > 0)
        {
            _statuses.RemoveRange(existing);
        }

        var entities = normalized
            .OrderBy(x => x.SortOrder)
            .Select((status, index) => new OrganizationWorkflowStatus
            {
                OrganizationId = organizationId,
                Category = JobLifecycleCategory,
                StatusKey = status.StatusKey,
                Label = status.Label,
                SortOrder = index
            })
            .ToList();

        _statuses.AddRange(entities);
        await _unitOfWork.SaveChangesAsync();

        var dto = entities
            .OrderBy(x => x.SortOrder)
            .Select(x => new WorkflowStatusDto
            {
                StatusKey = x.StatusKey,
                Label = x.Label,
                SortOrder = x.SortOrder
            })
            .ToList();

        return Result.Success(dto);
    }

    public async Task<Result<Dictionary<JobLifecycleStatus, string>>> GetJobLifecycleLabelMapAsync(Guid organizationId)
    {
        var listResult = await GetJobLifecycleStatusesAsync(organizationId);
        if (listResult.IsFailure)
        {
            return Result.Failure<Dictionary<JobLifecycleStatus, string>>(listResult.Error);
        }

        var map = new Dictionary<JobLifecycleStatus, string>();
        foreach (var status in listResult.Value)
        {
            if (Enum.TryParse<JobLifecycleStatus>(status.StatusKey, true, out var enumValue))
            {
                map[enumValue] = status.Label;
            }
        }

        return Result.Success(map);
    }

    private static List<WorkflowStatusDto> BuildDefaultJobLifecycleStatuses()
    {
        return Enum.GetValues<JobLifecycleStatus>()
            .OrderBy(x => (int)x)
            .Select((status, index) => new WorkflowStatusDto
            {
                StatusKey = status.ToString(),
                Label = SplitCamelCase(status.ToString()),
                SortOrder = index
            })
            .ToList();
    }

    private static string SplitCamelCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var buffer = new List<char>(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (i > 0 && char.IsUpper(current) && !char.IsWhiteSpace(value[i - 1]))
            {
                buffer.Add(' ');
            }

            buffer.Add(current);
        }

        return new string(buffer.ToArray());
    }
}
