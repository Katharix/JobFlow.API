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
    public class InvoiceService : IInvoiceService
    {
        private readonly ILogger<InvoiceService> logger;
        private readonly IUnitOfWork unitOfWork;
        private readonly IRepository<Invoice> invoices;

        public InvoiceService(ILogger<InvoiceService> logger, IUnitOfWork unitOfWork)
        {
            this.logger = logger;
            this.unitOfWork = unitOfWork;
            this.invoices = unitOfWork.RepositoryOf<Invoice>();
        }

        public async Task<Result<Invoice>> GetInvoiceByIdAsync(Guid id)
        {
            var invoice = await invoices.Query().FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null)
                return Result.Failure<Invoice>(InvoiceErrors.NotFound);

            return Result<Invoice>.Success(invoice);
        }

        public async Task<Result<IEnumerable<Invoice>>> GetInvoicesByClientAsync(Guid clientId)
        {
            var list = await invoices.Query().Where(i => i.OrganizationClientId == clientId).ToListAsync();
            return Result<IEnumerable<Invoice>>.Success(list.AsEnumerable());
        }

        public async Task<Result<Invoice>> UpsertInvoiceAsync(Invoice model)
        {
            var exists = await invoices.Query().AnyAsync(i => i.Id == model.Id);

            if (exists)
                invoices.Update(model);
            else
                await invoices.AddAsync(model);

            await unitOfWork.SaveChangesAsync();
            return Result<Invoice>.Success(model);
        }

        public async Task<Result> DeleteInvoiceAsync(Guid id)
        {
            var entity = await invoices.Query().FirstOrDefaultAsync(i => i.Id == id);
            if (entity == null)
                return Result.Failure(InvoiceErrors.NotFound);

            invoices.Remove(entity);
            await unitOfWork.SaveChangesAsync();
            return Result.Success();
        }
    }
}
