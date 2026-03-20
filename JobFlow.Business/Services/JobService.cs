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
public class JobService : IJobService
{
    private readonly IRepository<Job> jobs;
    private readonly ILogger<JobService> logger;
    private readonly IOnboardingService onboardingService;
    private readonly IUnitOfWork unitOfWork;
    private readonly IMapper _mapper;

    public JobService(
        ILogger<JobService> logger,
        IUnitOfWork unitOfWork,
        IOnboardingService onboardingService,
        IMapper mapper)
    {
        this.logger = logger;
        this.unitOfWork = unitOfWork;
        this.onboardingService = onboardingService;
        jobs = unitOfWork.RepositoryOf<Job>();
        _mapper = mapper;
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

    public async Task<Result<IEnumerable<JobDto>>> GetJobsAsync(Guid organizationId)
    {
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
            Title =  e.Title,
            Comments = e.Comments,
            LifecycleStatus = e.LifecycleStatus,
            Assignments = e.Assignments.Select(a => new AssignmentDto
            {
                ScheduledStart = a.ScheduledStart,
                ScheduledEnd = a.ScheduledEnd,
                ActualEnd = a.ActualEnd,
                ActualStart = a.ActualStart,
                Id =  a.Id,
                JobId =  e.Id,
                JobTitle =   e.Title,
                Status = a.Status,
                OrganizationClientId =  e.OrganizationClientId,
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
                OrganizationId =  e.OrganizationClient.OrganizationId,
                FirstName =  e.OrganizationClient.FirstName,
                LastName =  e.OrganizationClient.LastName,
                EmailAddress =  e.OrganizationClient.EmailAddress,
                PhoneNumber = e.OrganizationClient.PhoneNumber,
                Address1 =  e.OrganizationClient.Address1,
                Address2 = e.OrganizationClient.Address2,
                City =  e.OrganizationClient.City,
                State =  e.OrganizationClient.State,    
                ZipCode =  e.OrganizationClient.ZipCode
            }
        }).ToList();
        return Result.Success<IEnumerable<JobDto>>(dto);
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

        return Result.Success(job);
    }
}
