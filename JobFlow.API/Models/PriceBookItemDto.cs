using JobFlow.Domain.Enums;

namespace JobFlow.API.Models;

public record CreatePriceBookItemRequest(
    Guid? OrganizationId,
    string Name,
    string? Description,
    string? PartNumber,
    string? Unit,
    decimal Cost,
    decimal Price,
    PriceBookItemType Type,
    decimal InventoryUnitsPerSale,
    Guid? CategoryId
);

public record UpdatePriceBookItemRequest(
    Guid Id,
    Guid? OrganizationId,
    string Name,
    string? Description,
    string? PartNumber,
    string? Unit,
    decimal Cost,
    decimal Price,
    PriceBookItemType Type,
    decimal InventoryUnitsPerSale,
    Guid? CategoryId
);