using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Onboarding;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Business.Utilities;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class JobService : IJobService
{
    private readonly IRepository<Job> jobs;
    private readonly ILogger<JobService> logger;
    private readonly IOnboardingService onboardingService;
    private readonly IInvoicingSettingsService _invoicingSettings;
    private readonly IInvoiceService _invoiceService;
    private readonly IUnitOfWork unitOfWork;
    private readonly IMapper _mapper;
    private readonly IOrganizationRealtimeNotifier? _realtimeNotifier;

    public JobService(
        ILogger<JobService> logger,
        IUnitOfWork unitOfWork,
        IOnboardingService onboardingService,
        IInvoicingSettingsService invoicingSettings,
        IInvoiceService invoiceService,
        IMapper mapper,
        IOrganizationRealtimeNotifier? realtimeNotifier = null)
    {
        this.logger = logger;
        this.unitOfWork = unitOfWork;
        this.onboardingService = onboardingService;
        jobs = unitOfWork.RepositoryOf<Job>();
        _invoicingSettings = invoicingSettings;
        _invoiceService = invoiceService;
        _mapper = mapper;
        _realtimeNotifier = realtimeNotifier;
    }

    /// <summary>
    /// Heals jobs that have assignments but are still Draft/Approved by transitioning them to Booked.
    /// </summary>
    private async Task HealStaleJobStatusesAsync(Guid organizationId)
    {
        var staleJobs = await jobs.Query()
            .Where(j => j.OrganizationClient.OrganizationId == organizationId
                && (j.LifecycleStatus == JobLifecycleStatus.Draft || j.LifecycleStatus == JobLifecycleStatus.Approved)
                && j.Assignments.Any())
            .ToListAsync();

        if (staleJobs.Count == 0) return;

        foreach (var job in staleJobs)
        {
            job.LifecycleStatus = JobLifecycleStatus.Booked;
            jobs.Update(job);
        }

        await unitOfWork.SaveChangesAsync();
    }

    public async Task<Result<Job>> GetJobByIdAsync(Guid id, Guid organizationId)
    {
        var job = await jobs.Query()
            .Include(j => j.OrganizationClient)
            .FirstOrDefaultAsync(j =>
                j.Id == id &&
                j.OrganizationClient.OrganizationId == organizationId);

        if (job == null)
            return Result.Failure<Job>(JobErrors.NotFound);

        return Result.Success(job);
    }

    public async Task<Result<Job>> GetJobForClientAsync(
        Guid id,
        Guid organizationId,
        Guid organizationClientId)
    {
        var job = await jobs.Query()
            .Include(j => j.OrganizationClient)
            .FirstOrDefaultAsync(j =>
                j.Id == id &&
                j.OrganizationClient.OrganizationId == organizationId &&
                j.OrganizationClientId == organizationClientId);

        if (job == null)
            return Result.Failure<Job>(JobErrors.NotFound);

        return Result.Success(job);
    }

    public async Task<Result<IEnumerable<Job>>> GetJobsByStatusAsync(
        Guid organizationId,
        JobLifecycleStatus status)
    {
        var list = await jobs.Query()
            .Include(j => j.OrganizationClient)
            .Where(j =>
                j.LifecycleStatus == status &&
                j.OrganizationClient.OrganizationId == organizationId)
            .ToListAsync();

        return Result.Success<IEnumerable<Job>>(list);
    }

    public async Task<Result<IEnumerable<Job>>> GetJobsForClientAsync(
        Guid organizationId,
        Guid organizationClientId)
    {
        var list = await jobs.Query()
            .Include(j => j.OrganizationClient)
            .Where(j =>
                j.OrganizationClient.OrganizationId == organizationId &&
                j.OrganizationClientId == organizationClientId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        return Result.Success<IEnumerable<Job>>(list);
    }

    public async Task<Result<IEnumerable<JobDto>>> GetJobsAsync(Guid organizationId)
    {
        await HealStaleJobStatusesAsync(organizationId);

        var returnedJobs = await jobs.Query()
            .Include(j => j.OrganizationClient)
            .Include(e => e.Assignments)
            .ThenInclude(a => a.AssignmentAssignees)
            .ThenInclude(assignee => assignee.Employee)
            .Where(j => j.OrganizationClient.OrganizationId == organizationId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        var dto = returnedJobs.Select(e => new JobDto
        {
            Id = e.Id,
            OrganizationClientId = e.OrganizationClient.Id,
            Title = e.Title,
            Comments = e.Comments,
            LifecycleStatus = e.LifecycleStatus,
            InvoicingWorkflow = e.InvoicingWorkflow,
            Assignments = e.Assignments.Select(a => new AssignmentDto
            {
                ScheduledStart = a.ScheduledStart,
                ScheduledEnd = a.ScheduledEnd,
                ActualEnd = a.ActualEnd,
                ActualStart = a.ActualStart,
                Id = a.Id,
                JobId = e.Id,
                JobTitle = e.Title,
                Status = a.Status,
                OrganizationClientId = e.OrganizationClientId,
                JobLifecycleStatus = e.LifecycleStatus,
                Assignees = a.AssignmentAssignees
                    .Select(assignee => new AssignmentAssigneeDto
                    {
                        EmployeeId = assignee.EmployeeId,
                        EmployeeName = assignee.Employee != null
                            ? $"{assignee.Employee.FirstName} {assignee.Employee.LastName}".Trim()
                            : null,
                        IsLead = assignee.IsLead
                    })
                    .ToList()
            }),
            OrganizationClient = new OrganizationClientDto
            {
                OrganizationId = e.OrganizationClient.OrganizationId,
                FirstName = e.OrganizationClient.FirstName,
                LastName = e.OrganizationClient.LastName,
                EmailAddress = e.OrganizationClient.EmailAddress,
                PhoneNumber = e.OrganizationClient.PhoneNumber,
                Address1 = e.OrganizationClient.Address1,
                Address2 = e.OrganizationClient.Address2,
                City = e.OrganizationClient.City,
                State = e.OrganizationClient.State,
                ZipCode = e.OrganizationClient.ZipCode
            }
        }).ToList();
        return Result.Success<IEnumerable<JobDto>>(dto);
    }

    public async Task<Result<CursorPagedResponseDto<JobDto>>> GetJobsPagedAsync(
        Guid organizationId,
        int pageSize,
        string? cursor,
        string? statusKey,
        Guid? clientId,
        Guid? assigneeId,
        string? search,
        string? sortBy,
        string? sortDirection)
    {
        await HealStaleJobStatusesAsync(organizationId);

        var size = Math.Clamp(pageSize, 1, 100);
        var query = jobs.Query()
            .AsNoTracking()
            .Include(j => j.OrganizationClient)
            .Include(e => e.Assignments)
            .ThenInclude(a => a.AssignmentAssignees)
            .ThenInclude(assignee => assignee.Employee)
            .Where(j => j.OrganizationClient.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(statusKey)
            && Enum.TryParse<JobLifecycleStatus>(statusKey, true, out var parsedStatus))
        {
            query = query.Where(j => j.LifecycleStatus == parsedStatus);
        }

        if (clientId.HasValue && clientId.Value != Guid.Empty)
        {
            query = query.Where(j => j.OrganizationClientId == clientId.Value);
        }

        if (assigneeId.HasValue && assigneeId.Value != Guid.Empty)
        {
            query = query.Where(j => j.Assignments.Any(a => a.AssignmentAssignees.Any(aa => aa.EmployeeId == assigneeId.Value)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(j =>
                EF.Functions.Like(j.Title, $"%{term}%")
                || EF.Functions.Like(j.OrganizationClient.FirstName, $"%{term}%")
                || EF.Functions.Like(j.OrganizationClient.LastName, $"%{term}%")
                || (j.OrganizationClient.PhoneNumber != null && EF.Functions.Like(j.OrganizationClient.PhoneNumber, $"%{term}%")));
        }

        var desc = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy ?? string.Empty).ToLowerInvariant() switch
        {
            "title" => desc ? query.OrderByDescending(j => j.Title).ThenByDescending(j => j.Id) : query.OrderBy(j => j.Title).ThenBy(j => j.Id),
            "status" => desc ? query.OrderByDescending(j => j.LifecycleStatus).ThenByDescending(j => j.Id) : query.OrderBy(j => j.LifecycleStatus).ThenBy(j => j.Id),
            _ => desc ? query.OrderByDescending(j => j.CreatedAt).ThenByDescending(j => j.Id) : query.OrderBy(j => j.CreatedAt).ThenBy(j => j.Id)
        };

        var totalCount = await query.CountAsync();

        if (CursorToken.TryRead(cursor, out var cursorCreatedAt, out var cursorId))
        {
            query = query.Where(j => j.CreatedAt < cursorCreatedAt || (j.CreatedAt == cursorCreatedAt && j.Id.CompareTo(cursorId) < 0));
        }

        var batch = await query
            .Take(size + 1)
            .ToListAsync();

        var hasMore = batch.Count > size;
        var items = hasMore ? batch.Take(size).ToList() : batch;

        var mapped = items.Select(e => new JobDto
        {
            Id = e.Id,
            OrganizationClientId = e.OrganizationClient.Id,
            Title = e.Title,
            Comments = e.Comments,
            LifecycleStatus = e.LifecycleStatus,
            InvoicingWorkflow = e.InvoicingWorkflow,
            Assignments = e.Assignments.Select(a => new AssignmentDto
            {
                ScheduledStart = a.ScheduledStart,
                ScheduledEnd = a.ScheduledEnd,
                ActualEnd = a.ActualEnd,
                ActualStart = a.ActualStart,
                Id = a.Id,
                JobId = e.Id,
                JobTitle = e.Title,
                Status = a.Status,
                OrganizationClientId = e.OrganizationClientId,
                JobLifecycleStatus = e.LifecycleStatus,
                Assignees = a.AssignmentAssignees
                    .Select(assignee => new AssignmentAssigneeDto
                    {
                        EmployeeId = assignee.EmployeeId,
                        EmployeeName = assignee.Employee != null
                            ? $"{assignee.Employee.FirstName} {assignee.Employee.LastName}".Trim()
                            : null,
                        IsLead = assignee.IsLead
                    })
                    .ToList()
            }),
            OrganizationClient = new OrganizationClientDto
            {
                OrganizationId = e.OrganizationClient.OrganizationId,
                FirstName = e.OrganizationClient.FirstName,
                LastName = e.OrganizationClient.LastName,
                EmailAddress = e.OrganizationClient.EmailAddress,
                PhoneNumber = e.OrganizationClient.PhoneNumber,
                Address1 = e.OrganizationClient.Address1,
                Address2 = e.OrganizationClient.Address2,
                City = e.OrganizationClient.City,
                State = e.OrganizationClient.State,
                ZipCode = e.OrganizationClient.ZipCode
            }
        }).ToList();

        var nextCursor = hasMore && items.Count > 0
            ? CursorToken.Build(items[^1].CreatedAt, items[^1].Id)
            : null;

        return Result.Success(new CursorPagedResponseDto<JobDto>
        {
            Items = mapped,
            NextCursor = nextCursor,
            TotalCount = totalCount
        });
    }

    public async Task<Result<Job>> UpsertJobAsync(Job model, Guid organizationId)
    {
        var exists = await jobs.Query()
            .AnyAsync(j =>
                j.Id == model.Id &&
                j.OrganizationClient.OrganizationId == organizationId);

        if (exists)
        {
            var existingModel = await jobs.Query()
                .Include(j => j.OrganizationClient)
                .FirstAsync(j => j.Id == model.Id);

            // Explicitly DO NOT touch scheduling here
            existingModel.Title = model.Title;
            existingModel.Comments = model.Comments;
            existingModel.Latitude = model.Latitude;
            existingModel.Longitude = model.Longitude;
            existingModel.InvoicingWorkflow = model.InvoicingWorkflow;

            jobs.Update(existingModel);
        }
        else
        {
            model.LifecycleStatus = JobLifecycleStatus.Draft;
            await jobs.AddAsync(model);

            // Onboarding: job creation
            await onboardingService.MarkStepCompleteAsync(
                organizationId,
                OnboardingStepKeys.CreateJob
            );
        }

        await unitOfWork.SaveChangesAsync();
        return Result.Success(model);
    }

    public async Task<Result> DeleteJobAsync(Guid id)
    {
        var job = await jobs.Query()
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            return Result.Failure(JobErrors.NotFound);

        jobs.Remove(job);
        await unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Job>> UpdateJobStatusAsync(Guid organizationId, Guid jobId, JobLifecycleStatus status)
    {
        var job = await jobs.Query()
            .Include(j => j.OrganizationClient)
            .FirstOrDefaultAsync(j =>
                j.Id == jobId &&
                j.OrganizationClient.OrganizationId == organizationId);

        if (job == null)
            return Result.Failure<Job>(JobErrors.NotFound);

        job.LifecycleStatus = status;
        jobs.Update(job);
        await unitOfWork.SaveChangesAsync();

        if (_realtimeNotifier != null)
        {
            await _realtimeNotifier.NotifyJobStatusChangedAsync(
                organizationId, jobId, job.Title ?? string.Empty, status);
        }

        if (status == JobLifecycleStatus.Completed)
        {
            await HandleJobCompletedAsync(organizationId, job);
        }

        return Result.Success(job);
    }

    private async Task HandleJobCompletedAsync(Guid organizationId, Job job)
    {
        var workflow = job.InvoicingWorkflow;
        if (workflow == null)
        {
            var settingsResult = await _invoicingSettings.GetInvoicingSettingsAsync(organizationId);
            if (settingsResult.IsSuccess)
            {
                workflow = settingsResult.Value.DefaultWorkflow;
            }
        }

        if (workflow == null)
        {
            workflow = InvoicingWorkflow.SendInvoice;
        }

        if (workflow == InvoicingWorkflow.InPerson)
            return;

        var sendResult = await _invoiceService.SendInvoiceForJobAsync(organizationId, job);
        if (sendResult.IsFailure)
        {
            logger.LogWarning(
                "Invoice auto-send failed for completed job {JobId}: {Error}",
                job.Id,
                sendResult.Error.Description);
        }
    }

}
