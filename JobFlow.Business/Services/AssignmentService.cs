using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Onboarding;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class AssignmentService : IAssignmentService
{
    private readonly IRepository<Assignment> _assignments;
    private readonly IRepository<AssignmentAssignee> _assignmentAssignees;
    private readonly IRepository<Employee> _employees;
    private readonly IRepository<Job> _jobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IOnboardingService _onboardingService;
    private readonly IWorkflowSettingsService _workflowSettings;
    private readonly IScheduleSettingsService _scheduleSettings;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AssignmentService> _logger;

    public AssignmentService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IOnboardingService onboardingService,
        IWorkflowSettingsService workflowSettings,
        IScheduleSettingsService scheduleSettings,
        INotificationService notificationService,
        ILogger<AssignmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _assignments = unitOfWork.RepositoryOf<Assignment>();
        _assignmentAssignees = unitOfWork.RepositoryOf<AssignmentAssignee>();
        _employees = unitOfWork.RepositoryOf<Employee>();
        _jobs = unitOfWork.RepositoryOf<Job>();
        
        _mapper = mapper;
        _onboardingService = onboardingService;
        _workflowSettings = workflowSettings;
        _scheduleSettings = scheduleSettings;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result<AssignmentDto>> CreateAssignmentAsync(
        Guid organizationId,
        Guid jobId,
        CreateAssignmentRequestDto dto)
    {
        var validation = ValidateSchedule(dto.ScheduledStart, dto.ScheduledEnd, dto.ScheduleType);
        if (validation.IsFailure)
            return Result.Failure<AssignmentDto>(validation.Error);

        var job = await _jobs.Query()
            .Include(j => j.OrganizationClient)
            .FirstOrDefaultAsync(j =>
                j.Id == jobId &&
                j.OrganizationClient.OrganizationId == organizationId);

        if (job == null)
            return Result.Failure<AssignmentDto>(AssignmentErrors.JobNotFound);

        var assignment = new Assignment
        {
            JobId = jobId,
            ScheduleType = dto.ScheduleType,
            ScheduledStart = dto.ScheduledStart,
            ScheduledEnd = dto.ScheduledEnd,
            Address1 = dto.Address1,
            City = dto.City,
            State = dto.State,
            ZipCode = dto.ZipCode,
            Notes = dto.Notes
        };

        _assignments.Add(assignment);
        await _unitOfWork.SaveChangesAsync();

        await _onboardingService.MarkStepCompleteAsync(
            organizationId,
            OnboardingStepKeys.ScheduleJob
        );

        // Reload with navigation graph for DTO enrichment
        var created = await _assignments.Query()
            .Include(a => a.Job)
            .ThenInclude(j => j.OrganizationClient)
            .Include(a => a.AssignmentAssignees)
            .ThenInclude(assignee => assignee.Employee)
            .FirstAsync(a => a.Id == assignment.Id);

        return Result.Success(await MapToDtoAsync(organizationId, created));
    }

    public async Task<Result<AssignmentDto>> UpdateAssignmentScheduleAsync(
        Guid organizationId,
        Guid assignmentId,
        UpdateAssignmentScheduleRequestDto dto)
    {
        var validation = ValidateSchedule(dto.ScheduledStart, dto.ScheduledEnd, dto.ScheduleType);
        if (validation.IsFailure)
            return Result.Failure<AssignmentDto>(validation.Error);

        var assignment = await _assignments.Query()
            .Include(a => a.Job)
            .ThenInclude(j => j.OrganizationClient)
            .Include(a => a.AssignmentAssignees)
            .ThenInclude(assignee => assignee.Employee)
            .FirstOrDefaultAsync(a =>
                a.Id == assignmentId &&
                a.Job.OrganizationClient.OrganizationId == organizationId);

        if (assignment == null)
            return Result.Failure<AssignmentDto>(AssignmentErrors.NotFound);

        var originalStart = assignment.ScheduledStart;
        var originalEnd = assignment.ScheduledEnd;

        var scheduleSettings = await _scheduleSettings.GetScheduleSettingsAsync(organizationId);
        if (scheduleSettings.IsFailure)
            return Result.Failure<AssignmentDto>(scheduleSettings.Error);

        var conflictCheck = await EnsureNoBufferedConflictsAsync(
            organizationId,
            assignment,
            dto.ScheduledStart,
            dto.ScheduledEnd,
            scheduleSettings.Value);

        if (conflictCheck.IsFailure)
            return Result.Failure<AssignmentDto>(conflictCheck.Error);

        assignment.ScheduleType = dto.ScheduleType;
        assignment.ScheduledStart = dto.ScheduledStart;
        assignment.ScheduledEnd = dto.ScheduledEnd;

        _assignments.Update(assignment);
        await _unitOfWork.SaveChangesAsync();

        if (scheduleSettings.Value.AutoNotifyReschedule && ScheduleChanged(originalStart, originalEnd, assignment))
        {
            await TryNotifyRescheduleAsync(assignment, originalStart, originalEnd);
        }

        return Result.Success(await MapToDtoAsync(organizationId, assignment));
    }

    public async Task<Result<AssignmentDto>> UpdateAssignmentStatusAsync(
        Guid organizationId,
        Guid assignmentId,
        UpdateAssignmentStatusRequestDto dto)
    {
        var assignment = await _assignments.Query()
            .Include(a => a.Job)
            .ThenInclude(j => j.OrganizationClient)
            .Include(a => a.AssignmentAssignees)
            .ThenInclude(assignee => assignee.Employee)
            .FirstOrDefaultAsync(a =>
                a.Id == assignmentId &&
                a.Job.OrganizationClient.OrganizationId == organizationId);

        if (assignment == null)
            return Result.Failure<AssignmentDto>(AssignmentErrors.NotFound);

        assignment.Status = dto.Status;
        assignment.ActualStart ??= dto.ActualStart;
        assignment.ActualEnd ??= dto.ActualEnd;

        _assignments.Update(assignment);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(await MapToDtoAsync(organizationId, assignment));
    }

    public async Task<Result<AssignmentDto>> UpdateAssignmentAssigneesAsync(
        Guid organizationId,
        Guid assignmentId,
        UpdateAssignmentAssigneesRequestDto dto)
    {
        var assignment = await _assignments.Query()
            .Include(a => a.Job)
            .ThenInclude(j => j.OrganizationClient)
            .Include(a => a.AssignmentAssignees)
            .ThenInclude(assignee => assignee.Employee)
            .FirstOrDefaultAsync(a =>
                a.Id == assignmentId &&
                a.Job.OrganizationClient.OrganizationId == organizationId);

        if (assignment == null)
            return Result.Failure<AssignmentDto>(AssignmentErrors.NotFound);

        var requestedIds = (dto.EmployeeIds ?? new List<Guid>()).Distinct().ToList();
        if (requestedIds.Any())
        {
            var validEmployeeIds = await _employees.Query()
                .Where(e => e.OrganizationId == organizationId && requestedIds.Contains(e.Id))
                .Select(e => e.Id)
                .ToListAsync();

            if (validEmployeeIds.Count != requestedIds.Count)
                return Result.Failure<AssignmentDto>(AssignmentErrors.InvalidAssignee);

            requestedIds = validEmployeeIds;
        }

        if (assignment.AssignmentAssignees.Any())
        {
            _assignmentAssignees.RemoveRange(assignment.AssignmentAssignees);
        }

        var newAssignees = requestedIds.Select(id => new AssignmentAssignee
        {
            AssignmentId = assignmentId,
            EmployeeId = id,
            IsLead = dto.LeadEmployeeId.HasValue && dto.LeadEmployeeId.Value == id
        }).ToList();

        if (newAssignees.Any())
        {
            _assignmentAssignees.AddRange(newAssignees);
        }

        await _unitOfWork.SaveChangesAsync();

        var updated = await _assignments.Query()
            .Include(a => a.Job)
            .ThenInclude(j => j.OrganizationClient)
            .Include(a => a.AssignmentAssignees)
            .ThenInclude(assignee => assignee.Employee)
            .FirstAsync(a => a.Id == assignmentId);

        return Result.Success(await MapToDtoAsync(organizationId, updated));
    }

    public async Task<Result<AssignmentDto>> UpdateAssignmentNotesAsync(
        Guid organizationId,
        Guid assignmentId,
        UpdateAssignmentNotesRequestDto dto)
    {
        var assignment = await _assignments.Query()
            .Include(a => a.Job)
            .ThenInclude(j => j.OrganizationClient)
            .Include(a => a.AssignmentAssignees)
            .ThenInclude(assignee => assignee.Employee)
            .FirstOrDefaultAsync(a =>
                a.Id == assignmentId &&
                a.Job.OrganizationClient.OrganizationId == organizationId);

        if (assignment == null)
            return Result.Failure<AssignmentDto>(AssignmentErrors.NotFound);

        assignment.Notes = dto.Notes?.Trim();
        _assignments.Update(assignment);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(await MapToDtoAsync(organizationId, assignment));
    }

    public async Task<Result<List<AssignmentDto>>> GetAssignmentsAsync(
        Guid organizationId,
        DateTime start,
        DateTime end)
    {
        var assignments = await _assignments.Query()
            .Include(a => a.Job)
            .ThenInclude(j => j.OrganizationClient)
            .Include(a => a.AssignmentAssignees)
            .ThenInclude(assignee => assignee.Employee)
            .Where(a =>
                a.Job.OrganizationClient.OrganizationId == organizationId &&
                a.ScheduledStart < end &&
                (a.ScheduledEnd ?? a.ScheduledStart) >= start)
            .OrderBy(a => a.ScheduledStart)
            .ToListAsync();

        var labelMapResult = await _workflowSettings.GetJobLifecycleLabelMapAsync(organizationId);
        if (labelMapResult.IsFailure)
            return Result.Failure<List<AssignmentDto>>(labelMapResult.Error);

        var labelMap = labelMapResult.Value;
        var mapped = assignments.Select(a => MapToDto(a, labelMap)).ToList();

        return Result.Success(mapped);
    }

    public async Task<Result<AssignmentDto>> GetAssignmentByIdAsync(
        Guid organizationId,
        Guid assignmentId)
    {
        var assignment = await _assignments.Query()
            .Include(a => a.Job)
            .ThenInclude(j => j.OrganizationClient)
            .Include(a => a.AssignmentAssignees)
            .ThenInclude(assignee => assignee.Employee)
            .FirstOrDefaultAsync(a =>
                a.Id == assignmentId &&
                a.Job.OrganizationClient.OrganizationId == organizationId);

        if (assignment == null)
            return Result.Failure<AssignmentDto>(AssignmentErrors.NotFound);

        return Result.Success(await MapToDtoAsync(organizationId, assignment));
    }

    private async Task<AssignmentDto> MapToDtoAsync(Guid organizationId, Assignment assignment)
    {
        var labelMapResult = await _workflowSettings.GetJobLifecycleLabelMapAsync(organizationId);
        var labelMap = labelMapResult.IsSuccess ? labelMapResult.Value : new Dictionary<JobLifecycleStatus, string>();

        return MapToDto(assignment, labelMap);
    }

    private AssignmentDto MapToDto(Assignment assignment, Dictionary<JobLifecycleStatus, string> labelMap)
    {
        var dto = _mapper.Map<AssignmentDto>(assignment);

        // UI enrichment (kept here intentionally)
        dto.JobTitle = assignment.Job?.Title;
        dto.OrganizationClientId = assignment.Job?.OrganizationClientId ?? Guid.Empty;
        dto.ClientName = assignment.Job?.OrganizationClient != null
            ? $"{assignment.Job.OrganizationClient.FirstName} {assignment.Job.OrganizationClient.LastName}"
            : null;
        dto.JobLifecycleStatus = assignment.Job?.LifecycleStatus ?? JobLifecycleStatus.Draft;
        if (labelMap.TryGetValue(dto.JobLifecycleStatus, out var label))
        {
            dto.StatusLabel = label;
        }
        dto.Assignees = assignment.AssignmentAssignees
            .Select(assignee => new AssignmentAssigneeDto
            {
                EmployeeId = assignee.EmployeeId,
                EmployeeName = assignee.Employee != null
                    ? $"{assignee.Employee.FirstName} {assignee.Employee.LastName}".Trim()
                    : null,
                IsLead = assignee.IsLead
            })
            .ToList();

        return dto;
    }

    private static Result ValidateSchedule(DateTimeOffset scheduledStart, DateTimeOffset? scheduledEnd, ScheduleType scheduleType)
    {
        if (scheduledStart == default)
        {
            return Result.Failure(AssignmentErrors.ScheduledStartRequired);
        }

        if (scheduleType == ScheduleType.Window && !scheduledEnd.HasValue)
        {
            return Result.Failure(AssignmentErrors.ScheduledEndRequiredForWindow);
        }

        if (scheduledEnd.HasValue && scheduledEnd.Value <= scheduledStart)
        {
            return Result.Failure(AssignmentErrors.ScheduledEndMustBeAfterStart);
        }

        return Result.Success();
    }

    private async Task<Result> EnsureNoBufferedConflictsAsync(
        Guid organizationId,
        Assignment assignment,
        DateTimeOffset newStart,
        DateTimeOffset? newEnd,
        ScheduleSettingsDto settings)
    {
        if (!settings.EnforceTravelBuffer)
            return Result.Success();

        var assigneeIds = assignment.AssignmentAssignees
            .Select(x => x.EmployeeId)
            .Distinct()
            .ToList();

        if (assigneeIds.Count == 0)
            return Result.Success();

        var bufferMinutes = settings.TravelBufferMinutes;
        var newRangeStart = newStart.AddMinutes(-bufferMinutes);
        var newRangeEnd = (newEnd ?? newStart).AddMinutes(bufferMinutes);

        var candidateAssignments = await _assignments.Query()
            .Include(a => a.AssignmentAssignees)
            .Include(a => a.Job)
            .ThenInclude(j => j.OrganizationClient)
            .Where(a =>
                a.Id != assignment.Id &&
                a.Job.OrganizationClient.OrganizationId == organizationId &&
                a.AssignmentAssignees.Any(assignee => assigneeIds.Contains(assignee.EmployeeId)))
            .ToListAsync();

        foreach (var other in candidateAssignments)
        {
            var otherStart = other.ScheduledStart;
            var otherEnd = other.ScheduledEnd ?? other.ScheduledStart;
            var otherRangeStart = otherStart.AddMinutes(-bufferMinutes);
            var otherRangeEnd = otherEnd.AddMinutes(bufferMinutes);

            if (RangesOverlap(newRangeStart, newRangeEnd, otherRangeStart, otherRangeEnd))
            {
                return Result.Failure(AssignmentErrors.ScheduleConflictWithBuffer);
            }
        }

        return Result.Success();
    }

    private static bool RangesOverlap(DateTimeOffset startA, DateTimeOffset endA, DateTimeOffset startB, DateTimeOffset endB)
    {
        return startA < endB && startB < endA;
    }

    private static bool ScheduleChanged(DateTimeOffset originalStart, DateTimeOffset? originalEnd, Assignment assignment)
    {
        if (originalStart != assignment.ScheduledStart)
            return true;

        var originalEndValue = originalEnd ?? originalStart;
        var updatedEndValue = assignment.ScheduledEnd ?? assignment.ScheduledStart;

        return originalEndValue != updatedEndValue;
    }

    private async Task TryNotifyRescheduleAsync(Assignment assignment, DateTimeOffset oldStart, DateTimeOffset? oldEnd)
    {
        try
        {
            if (assignment.Job?.OrganizationClient == null)
                return;

            await _notificationService.SendClientJobRescheduledNotificationAsync(
                assignment.Job.OrganizationClient,
                assignment.Job,
                oldStart,
                oldEnd,
                assignment.ScheduledStart,
                assignment.ScheduledEnd);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reschedule notification for assignment {AssignmentId}", assignment.Id);
        }
    }
}
