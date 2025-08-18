using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IPriceBookCategoryService
    {
        Task<Result<PriceBookCategory>> GetByIdAsync(Guid id);
        Task<Result<List<PriceBookCategory>>> GetAllAsync(Guid organizationId);
        Task<Result<PriceBookCategory>> CreateAsync(PriceBookCategory category);
        Task<Result<PriceBookCategory>> UpdateAsync(PriceBookCategory category);
        Task<Result> DeleteAsync(Guid id);
    }
}
