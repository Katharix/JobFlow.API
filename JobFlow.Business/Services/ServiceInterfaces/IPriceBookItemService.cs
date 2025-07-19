using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IPriceBookItemService
    { 
        Task<Result<PriceBookItem>> GetByIdAsync(int id);
        Task<Result<List<PriceBookItem>>> GetAllAsync(Guid organizationId);
        Task<Result<PriceBookItem>> CreateAsync(PriceBookItem item);
        Task<Result<PriceBookItem>> UpdateAsync(PriceBookItem item);
        Task<Result> DeleteAsync(int id);
    }
}
