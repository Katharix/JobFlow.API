using JobFlow.API.Models;
using JobFlow.Business.Extensions;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/pricebook")]
public class PriceBookController : ControllerBase
{
    private readonly IPriceBookItemService _service;

    public PriceBookController(IPriceBookItemService service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    public async Task<IResult> Get(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("org/{orgId}/category/{categoryId}")]
    public async Task<IResult> GetAllByCategory(Guid orgId, Guid categoryId)
    {
        var result = await _service.GetAllAsync(orgId, categoryId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    public async Task<IResult> Create([FromBody] CreatePriceBookItemRequest dto)
    {
        var item = new PriceBookItem
        {
            OrganizationId = dto.OrganizationId,
            Name = dto.Name,
            Description = dto.Description,
            Unit = dto.Unit,
            PricePerUnit = dto.Cost,
            Price = dto.Price,
            Cost = dto.Cost,
            PartNumber = dto.PartNumber,
            ItemType = dto.Type,
            InventoryUnitsPerSale = dto.InventoryUnitsPerSale,
            CategoryId = dto.CategoryId
        };
        var result = await _service.CreateAsync(item);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut]
    public async Task<IResult> Update([FromBody] UpdatePriceBookItemRequest dto)
    {
        var item = new PriceBookItem
        {
            Id = dto.Id,
            OrganizationId = dto.OrganizationId,
            Name = dto.Name,
            Description = dto.Description,
            Unit = dto.Unit,
            PricePerUnit = dto.Cost,
            ItemType = dto.Type,
            InventoryUnitsPerSale = dto.InventoryUnitsPerSale,
            CategoryId = dto.CategoryId
        };
        var result = await _service.UpdateAsync(item);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpDelete("{id}")]
    public async Task<IResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}