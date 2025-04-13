using JobFlow.Business.ModelErrors;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.DI;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services
{
    [ScopedService]
    public class JobOrderService : IJobOrderService
    {
        private readonly ILogger<JobOrderService> logger;
        private readonly IUnitOfWork unitOfWork;
        private readonly IRepository<JobOrder> jobOrders;
        private readonly IRepository<Job> jobs;
        private readonly IRepository<Order> orders;

        public JobOrderService(ILogger<JobOrderService> logger, IUnitOfWork unitOfWork)
        {
            this.logger = logger;
            this.unitOfWork = unitOfWork;
            this.jobOrders = unitOfWork.RepositoryOf<JobOrder>();
            this.jobs = unitOfWork.RepositoryOf<Job>();
            this.orders = unitOfWork.RepositoryOf<Order>();
        }

        public async Task<Result> AddJobToOrderAsync(Guid jobId, Guid orderId)
        {
            var jobExists = await jobs.Query().AnyAsync(j => j.Id == jobId);
            if (!jobExists)
                return Result.Failure(JobOrderErrors.JobNotFound);

            var orderExists = await orders.Query().AnyAsync(o => o.Id == orderId);
            if (!orderExists)
                return Result.Failure(JobOrderErrors.OrderNotFound);

            var alreadyLinked = await jobOrders.Query().AnyAsync(x => x.JobId == jobId && x.OrderId == orderId);
            if (alreadyLinked)
                return Result.Failure(JobOrderErrors.JobAlreadyLinkedToOrder);

            var entity = new JobOrder
            {
                JobId = jobId,
                OrderId = orderId,
                CreatedAt = DateTime.UtcNow
            };

            await jobOrders.AddAsync(entity);
            await unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> RemoveJobFromOrderAsync(Guid jobId, Guid orderId)
        {
            var existing = await jobOrders.Query().FirstOrDefaultAsync(x => x.JobId == jobId && x.OrderId == orderId);
            if (existing == null)
                return Result.Failure(JobOrderErrors.JobOrderLinkNotFound);

            jobOrders.Remove(existing);
            await unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result<List<Guid>>> GetJobIdsForOrderAsync(Guid orderId)
        {
            var orderExists = await orders.Query().AnyAsync(o => o.Id == orderId);
            if (!orderExists)
                return Result.Failure<List<Guid>>(JobOrderErrors.OrderNotFound);

            var jobIds = await jobOrders.Query()
                .Where(x => x.OrderId == orderId)
                .Select(x => x.JobId)
                .ToListAsync();

            return Result<List<Guid>>.Success(jobIds);
        }
    }
}
