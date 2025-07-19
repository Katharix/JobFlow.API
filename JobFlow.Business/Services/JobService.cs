using JobFlow.Business.ModelErrors;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using JobFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using JobFlow.Business.DI;

namespace JobFlow.Business.Services
{
    [ScopedService]
    public class JobService : IJobService
    {
        private readonly ILogger<JobService> logger;
        private readonly IUnitOfWork unitOfWork;
        private readonly IRepository<Job> jobs;

        public JobService(ILogger<JobService> logger, IUnitOfWork unitOfWork)
        {
            this.logger = logger;
            this.unitOfWork = unitOfWork;
            this.jobs = unitOfWork.RepositoryOf<Job>();
        }

        public async Task<Result<Job>> GetJobByIdAsync(Guid id)
        {
            var job = await jobs.Query().FirstOrDefaultAsync(j => j.Id == id);
            if (job == null)
                return Result.Failure<Job>(JobErrors.NotFound);

            return Result<Job>.Success(job);
        }

        public async Task<Result<IEnumerable<Job>>> GetJobsByStatusAsync(Guid statusId)
        {
            var list = await jobs.Query().Where(j => j.JobStatusId == statusId).ToListAsync();
            return Result<IEnumerable<Job>>.Success(list.AsEnumerable());
        }

        public async Task<Result<Job>> UpsertJobAsync(Job model)
        {
            var exists = await jobs.Query().AnyAsync(j => j.Id == model.Id);

            if (exists)
                jobs.Update(model);
            else
                await jobs.AddAsync(model);

            await unitOfWork.SaveChangesAsync();
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
                .Where(j => j.ScheduledDate >= start && j.ScheduledDate < end)
                .ToListAsync();

            return Result<IEnumerable<Job>>.Success(list.AsEnumerable());
        }
    }

}
