
using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Domain.Models;
using JobFlow.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using global::JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services
{
    [ScopedService]
    public class PriceBookCategoryService : IPriceBookCategoryService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<PriceBookCategoryService> _logger;

        public PriceBookCategoryService(IUnitOfWork uow, ILogger<PriceBookCategoryService> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<Result<PriceBookCategory>> GetByIdAsync(Guid id)
        {
            var entity = await _uow.RepositoryOf<PriceBookCategory>()
                .Query()
                .FirstOrDefaultAsync(e => e.Id == id);

            return entity is null
                ? Result.Failure<PriceBookCategory>(PriceBookCategoryErrors.PriceBookCategoryNotFound)
                : Result.Success(entity);
        }

        public async Task<Result<List<PriceBookCategoryDto>>> GetAllAsync(Guid organizationId)
        {
            var items = await _uow.RepositoryOf<PriceBookCategory>()
                .Query()
                .AsNoTracking()
                .Include(e => e.Items)
                .Where(x => x.OrganizationId == organizationId)
                .Select(e => new PriceBookCategoryDto
                {
                    Id = e.Id,
                    OrganizationId = organizationId,
                    Description = e.Description,
                    ItemCount = e.Items.Count(),
                    Name = e.Name
                })
                .OrderBy(x => x.Name)
                .ToListAsync();
            return items;
        }

        public async Task<Result<PriceBookCategory>> CreateAsync(PriceBookCategory category)
        {
            // Enforce unique name per org (case-insensitive)
            var exists = await _uow.RepositoryOf<PriceBookCategory>()
                .Query()
                .AnyAsync(x => x.OrganizationId == category.OrganizationId && x.Name.ToLower() == category.Name.ToLower());
            if (exists) return Result.Failure<PriceBookCategory>(PriceBookCategoryErrors.PriceBookCategoryNameExists);

            await _uow.RepositoryOf<PriceBookCategory>().AddAsync(category);
            await _uow.SaveChangesAsync();
            return category;
        }

        public async Task<Result<PriceBookCategory>> UpdateAsync(PriceBookCategory category)
        {
            var existing = await _uow.RepositoryOf<PriceBookCategory>()
                .Query()
                .FirstOrDefaultAsync(e => e.Id == category.Id);
            if (existing is null)
                return Result.Failure<PriceBookCategory>(PriceBookCategoryErrors.PriceBookCategoryNotFound);

            var nameTaken = await _uow.RepositoryOf<PriceBookCategory>()
                .Query()
                .AnyAsync(x => x.OrganizationId == existing.OrganizationId && x.Id != existing.Id && x.Name.ToLower() == category.Name.ToLower());
            if (nameTaken)
                return Result.Failure<PriceBookCategory>(PriceBookCategoryErrors.PriceBookCategoryNameExists);

            existing.Name = category.Name;
            existing.Description = category.Description;

            _uow.RepositoryOf<PriceBookCategory>().Update(existing);
            await _uow.SaveChangesAsync();
            return Result.Success(existing);
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            var entity = await _uow.RepositoryOf<PriceBookCategory>()
                .Query()
                .FirstOrDefaultAsync(e => e.Id == id);
            if (entity is null)
                return Result.Failure(PriceBookCategoryErrors.PriceBookCategoryNotFound);

            _uow.RepositoryOf<PriceBookCategory>().Remove(entity);
            await _uow.SaveChangesAsync();
            return Result.Success();
        }
    }
}

