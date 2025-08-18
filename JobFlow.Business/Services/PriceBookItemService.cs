using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Domain.Models;
using JobFlow.Domain;
using Microsoft.Extensions.Logging;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Business.Services
{
    [ScopedService]
    public class PriceBookItemService : IPriceBookItemService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<PriceBookItemService> _logger;

        public PriceBookItemService(IUnitOfWork uow, ILogger<PriceBookItemService> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<Result<PriceBookItem>> GetByIdAsync(Guid id)
        {
            var item = await _uow.RepositoryOf<PriceBookItem>().Query().FirstOrDefaultAsync(e => e.Id == id);
            return item is null ? Result.Failure<PriceBookItem>(PriceBookErrors.PriceBookItemNotFound) : Result.Success(item);
        }

        public async Task<Result<List<PriceBookItem>>> GetAllAsync(Guid organizationId, Guid? categoryId = null)
        {
            var q = _uow.RepositoryOf<PriceBookItem>().Query()
                .Where(x => x.OrganizationId == organizationId);

            if (categoryId.HasValue)
                q = q.Where(x => x.CategoryId == categoryId.Value);

            var items = await q.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
            return items;
        }

        public async Task<Result<PriceBookItem>> CreateAsync(PriceBookItem item)
        {
            await _uow.RepositoryOf<PriceBookItem>().AddAsync(item);
            await _uow.SaveChangesAsync();
            return item;
        }

        public async Task<Result<PriceBookItem>> UpdateAsync(PriceBookItem item)
        {
            var existing = await _uow.RepositoryOf<PriceBookItem>().Query().FirstOrDefaultAsync(e => e.Id == item.Id);
            if (existing is null) return Result.Failure<PriceBookItem>(PriceBookErrors.PriceBookItemNotFound);

            _uow.RepositoryOf<PriceBookItem>().Update(item);
            await _uow.SaveChangesAsync();
            return Result.Success(item);
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            var item = await _uow.RepositoryOf<PriceBookItem>().Query().FirstOrDefaultAsync(e => e.Id == id);
            if (item is null) return Result.Failure(PriceBookErrors.PriceBookItemNotFound);

            _uow.RepositoryOf<PriceBookItem>().Remove(item);
            await _uow.SaveChangesAsync();
            return Result.Success();
        }
    }
}
