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
    public class PaymentHistoryService : IPaymentHistoryService
    {
        private readonly ILogger<PaymentHistoryService> logger;
        private readonly IUnitOfWork unitOfWork;
        private readonly IRepository<PaymentHistory> paymentHistory;

        public PaymentHistoryService(ILogger<PaymentHistoryService> logger, IUnitOfWork unitOfWork)
        {
            this.logger = logger;
            this.unitOfWork = unitOfWork;
            this.paymentHistory = unitOfWork.RepositoryOf<PaymentHistory>();
        }

        public async Task<Result> LogAsync(PaymentHistory history)
        {
            await paymentHistory.AddAsync(history);
            await unitOfWork.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result<List<PaymentHistory>>> GetPaymentsForEntityAsync(Guid entityId)
        {
            var records = await paymentHistory.Query()
                .Where(p => p.EntityId == entityId)
                .ToListAsync();

            return Result<List<PaymentHistory>>.Success(records);
        }
    }
}
