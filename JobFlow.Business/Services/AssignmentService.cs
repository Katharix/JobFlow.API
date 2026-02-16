using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class AssignmentService : IAssignmentService
{
    private readonly IRepository<Assignment> _assignments;
    private readonly IRepository<Job> _jobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AssignmentService> _logger;

    public AssignmentService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AssignmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _assignments = unitOfWork.RepositoryOf<Assignment>();
        _jobs = unitOfWork.RepositoryOf<Job>();
        
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<AssignmentDto>> CreateAssignmentAsync(
        Guid organizationId,
        Guid jobId,
        CreateAssignmentRequestDto dto)
    {
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

        // Reload with navigation graph for DTO enrichment
        var created = await _assignments.Query()
            .Include(a => a.Job)
            .ThenInclude(j => j.OrganizationClient)
            .FirstAsync(a => a.Id == assignment.Id);

        return Result.Success(MapToDto(created));
    }

    public async Task<Result<AssignmentDto>> UpdateAssignmentScheduleAsync(
        Guid organizationId,
        Guid assignmentId,
        UpdateAssignmentScheduleRequestDto dto)
    {
        var assignment = await _assignments.Query()
            .Include(a => a.Job)
            .ThenInclude(j => j.OrganizationClient)
            .FirstOrDefaultAsync(a =>
                a.Id == assignmentId &&
                a.Job.OrganizationClient.OrganizationId == organizationId);

        if (assignment == null)
            return Result.Failure<AssignmentDto>(AssignmentErrors.NotFound);

        assignment.ScheduleType = dto.ScheduleType;
        assignment.ScheduledStart = dto.ScheduledStart;
        assignment.ScheduledEnd = dto.ScheduledEnd;

        _assignments.Update(assignment);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(MapToDto(assignment));
    }

    public async Task<Result<AssignmentDto>> UpdateAssignmentStatusAsync(
        Guid organizationId,
        Guid assignmentId,
        UpdateAssignmentStatusRequestDto dto)
    {
        var assignment = await _assignments.Query()
            .Include(a => a.Job)
            .ThenInclude(j => j.OrganizationClient)
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

        return Result.Success(MapToDto(assignment));
    }

    public async Task<Result<List<AssignmentDto>>> GetAssignmentsAsync(
        Guid organizationId,
        DateTime start,
        DateTime end)
    {
        var assignments = await _assignments.Query()
            .Include(a => a.Job)
            .ThenInclude(j => j.OrganizationClient)
            .Where(a =>
                a.Job.OrganizationClient.OrganizationId == organizationId &&
                a.ScheduledStart < end &&
                (a.ScheduledEnd ?? a.ScheduledStart) >= start)
            .OrderBy(a => a.ScheduledStart)
            .ToListAsync();

        return Result.Success(assignments.Select(MapToDto).ToList());
    }

    public async Task<Result<AssignmentDto>> GetAssignmentByIdAsync(
        Guid organizationId,
        Guid assignmentId)
    {
        var assignment = await _assignments.Query()
            .Include(a => a.Job)
            .ThenInclude(j => j.OrganizationClient)
            .FirstOrDefaultAsync(a =>
                a.Id == assignmentId &&
                a.Job.OrganizationClient.OrganizationId == organizationId);

        if (assignment == null)
            return Result.Failure<AssignmentDto>(AssignmentErrors.NotFound);

        return Result.Success(MapToDto(assignment));
    }

    private AssignmentDto MapToDto(Assignment assignment)
    {
        var dto = _mapper.Map<AssignmentDto>(assignment);

        // UI enrichment (kept here intentionally)
        dto.JobTitle = assignment.Job?.Title;
        dto.OrganizationClientId = assignment.Job?.OrganizationClientId ?? Guid.Empty;
        dto.ClientName = assignment.Job?.OrganizationClient != null
            ? $"{assignment.Job.OrganizationClient.FirstName} {assignment.Job.OrganizationClient.LastName}"
            : null;

        return dto;
    }
}
