using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services
{
    [ScopedService]
    public class InvoiceLineItemService : IInvoiceLineItemService
    {
        private readonly IRepository<InvoiceLineItem> items;
        private readonly IUnitOfWork unitOfWork;

        public InvoiceLineItemService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
            this.items = unitOfWork.RepositoryOf<InvoiceLineItem>();
        }

        public async Task<Result<IEnumerable<InvoiceLineItem>>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            var list = await items.Query().Where(x => x.InvoiceId == invoiceId).ToListAsync();
            return Result<IEnumerable<InvoiceLineItem>>.Success(list.AsEnumerable());
        }

        public async Task<Result> DeleteByInvoiceIdAsync(Guid invoiceId)
        {
            var list = await items.Query().Where(x => x.InvoiceId == invoiceId).ToListAsync();
            foreach (var item in list)
                items.Remove(item);

            await unitOfWork.SaveChangesAsync();
            return Result.Success();
        }
    }

}
