using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IInventoryService
{
    Task<Result<InventoryItem>> GetByIdAsync(Guid id);
    Task<Result<List<InventoryItem>>> GetAllAsync(Guid organizationId);
    Task<Result<InventoryItem>> CreateAsync(InventoryItem item);
    Task<Result<InventoryItem>> UpdateAsync(InventoryItem item);
    Task<Result> DeleteAsync(Guid id);
}