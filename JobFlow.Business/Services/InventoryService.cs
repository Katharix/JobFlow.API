using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using JobFlow.Domain;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Business.Services
{
    [ScopedService]
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(IUnitOfWork uow, ILogger<InventoryService> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<Result<InventoryItem>> GetByIdAsync(Guid id)
        {
            var item = await _uow.RepositoryOf<InventoryItem>().Query().FirstOrDefaultAsync(e => e.Id == id);
            return item is null ? Result.Failure<InventoryItem>(InventoryErrors.InventoryItemNotFound) : Result.Success(item);
        }

        public async Task<Result<List<InventoryItem>>> GetAllAsync(Guid organizationId)
        {
            var items = await _uow.RepositoryOf<InventoryItem>().Query().Where(x => x.OrganizationId == organizationId).ToListAsync();
            return items;
        }

        public async Task<Result<InventoryItem>> CreateAsync(InventoryItem item)
        {
            await _uow.RepositoryOf<InventoryItem>().AddAsync(item);
            await _uow.SaveChangesAsync();
            return item;
        }

        public async Task<Result<InventoryItem>> UpdateAsync(InventoryItem item)
        {
            var existing = await _uow.RepositoryOf<InventoryItem>().Query().FirstOrDefaultAsync(e => e.Id == item.Id);
            if (existing is null) return Result.Failure<InventoryItem>(InventoryErrors.InventoryItemNotFound);

            _uow.RepositoryOf<InventoryItem>().Update(item);
            await _uow.SaveChangesAsync();
            return Result.Success(item);
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            var item = await _uow.RepositoryOf<InventoryItem>().Query().FirstOrDefaultAsync(e => e.Id == id);
            if (item is null) return Result.Failure<InventoryItem>(InventoryErrors.InventoryItemNotFound);

            _uow.RepositoryOf<InventoryItem>().Remove(item);
            await _uow.SaveChangesAsync();
            return Result.Success();
        }
    }
}
