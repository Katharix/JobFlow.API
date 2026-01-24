using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.Web.Controllers;

[ApiController]
[Route("api/pricebook/categories")]
public class PriceBookCategoriesController : ControllerBase
{
    private readonly IPriceBookCategoryService _service;

    public PriceBookCategoriesController(IPriceBookCategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IResult> GetAll()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _service.GetAllAsync(organizationId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id:Guid}")]
    public async Task<IResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    public async Task<IResult> Create([FromBody] PriceBookCategory body)
    {
        var organizationId = HttpContext.GetOrganizationId();
        body.OrganizationId = organizationId;
        var result = await _service.CreateAsync(body);
        if (!result.IsSuccess) return result.ToProblemDetails();
        var location = $"/api/pricebook/categories/{result.Value!.Id}";
        return Results.Created(location, result.Value);
    }

    [HttpPut("{id:Guid}")]
    public async Task<IResult> Update(Guid id, [FromBody] PriceBookCategory body)
    {
        var organizationId = HttpContext.GetOrganizationId();
        body.Id = id;
        body.OrganizationId = organizationId;
        var result = await _service.UpdateAsync(body);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpDelete("{id:Guid}")]
    public async Task<IResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}