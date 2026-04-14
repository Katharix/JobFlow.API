using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _service;

    public InventoryController(IInventoryService service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    public async Task<IResult> Get(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _service.GetByIdAsync(id);
        if (!result.IsSuccess)
            return result.ToProblemDetails();
        if (result.Value.OrganizationId != organizationId)
            return Results.NotFound();
        return Results.Ok(result.Value);
    }

    [HttpGet("org/{orgId}")]
    public async Task<IResult> GetAll(Guid orgId)
    {
        var organizationId = HttpContext.GetOrganizationId();
        if (orgId != organizationId)
            return Results.Forbid();
        var result = await _service.GetAllAsync(orgId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    public async Task<IResult> Create([FromBody] InventoryItem item)
    {
        var organizationId = HttpContext.GetOrganizationId();
        item.OrganizationId = organizationId;
        var result = await _service.CreateAsync(item);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut]
    public async Task<IResult> Update([FromBody] InventoryItem item)
    {
        var organizationId = HttpContext.GetOrganizationId();
        item.OrganizationId = organizationId;
        var result = await _service.UpdateAsync(item);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpDelete("{id}")]
    public async Task<IResult> Delete(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var existing = await _service.GetByIdAsync(id);
        if (!existing.IsSuccess)
            return existing.ToProblemDetails();
        if (existing.Value.OrganizationId != organizationId)
            return Results.NotFound();
        var result = await _service.DeleteAsync(id);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}