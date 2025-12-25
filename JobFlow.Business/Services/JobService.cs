using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Onboarding;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
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

    public JobService(
        ILogger<JobService> logger,
        IUnitOfWork unitOfWork,
        IOnboardingService onboardingService)
    {
        this.logger = logger;
        this.unitOfWork = unitOfWork;
        this.onboardingService = onboardingService;
        jobs = unitOfWork.RepositoryOf<Job>();
    }

    public async Task<Result<Job>> GetJobByIdAsync(Guid id, Guid organizationId)
    {
        var job = await jobs.Query().FirstOrDefaultAsync(j => j.Id == id);
        if (job == null)
            return Result.Failure<Job>(JobErrors.NotFound);

        return Result<Job>.Success(job);
    }

    public async Task<Result<IEnumerable<Job>>> GetJobsByStatusAsync(Guid organizationId, Guid statusId)
    {
        var list = await jobs.Query().Where(j => j.JobStatusId == statusId).ToListAsync();
        return Result<IEnumerable<Job>>.Success(list.AsEnumerable());
    }

    public async Task<Result<Job>> UpsertJobAsync(Job model, Guid organizationId)
    {
        var exists = await jobs.Query().AnyAsync(j => j.Id == model.Id);

        if (exists)
            jobs.Update(model);
        else
            await jobs.AddAsync(model);

        await unitOfWork.SaveChangesAsync();
        if (!exists)
        {
            await onboardingService.MarkStepCompleteAsync(
                organizationId,
                OnboardingStepKeys.CreateJob
            );
        }

        return Result<Job>.Success(model);
    }

    public async Task<Result> DeleteJobAsync(Guid id)
    {
        var job = await jobs.Query().FirstOrDefaultAsync(j => j.Id == id);
        if (job == null)
            return Result.Failure(JobErrors.NotFound);

        jobs.Remove(job);
        await unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<IEnumerable<Job>>> GetJobsByDate(DateTime date)
    {
        // Define the start and end of the day
        var start = date.Date;
        var end = start.AddDays(1);

        // Fetch jobs scheduled within that day
        var list = await jobs.Query()
            .Where(j => j.ScheduledStart >= start && j.ScheduledStart < end)
            .ToListAsync();

        return Result<IEnumerable<Job>>.Success(list.AsEnumerable());
    }
}