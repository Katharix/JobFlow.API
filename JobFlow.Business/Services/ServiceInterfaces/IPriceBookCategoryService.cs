using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IPriceBookCategoryService
{
    Task<Result<PriceBookCategory>> GetByIdAsync(Guid id);
    Task<Result<List<PriceBookCategoryDto>>> GetAllAsync(Guid organizationId);
    Task<Result<PriceBookCategory>> CreateAsync(PriceBookCategory category);
    Task<Result<PriceBookCategory>> UpdateAsync(PriceBookCategory category);
    Task<Result> DeleteAsync(Guid id);
}