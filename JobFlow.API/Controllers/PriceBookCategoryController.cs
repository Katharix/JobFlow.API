using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Business.Extensions;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.Web.Controllers
{
    [ApiController]
    [Route("api/organizations/{organizationId:guid}/pricebook/categories")]
    public class PriceBookCategoriesController : ControllerBase
    {
        private readonly IPriceBookCategoryService _service;
        public PriceBookCategoriesController(IPriceBookCategoryService service) => _service = service;

        [HttpGet]
        public async Task<IResult> GetAll(Guid organizationId)
        {
            var result = await _service.GetAllAsync(organizationId);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpGet("{id:int}")]
        public async Task<IResult> GetById(Guid organizationId, Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpPost]
        public async Task<IResult> Create(Guid organizationId, [FromBody] PriceBookCategory body)
        {
            body.OrganizationId = organizationId;
            var result = await _service.CreateAsync(body);
            if (!result.IsSuccess) return result.ToProblemDetails();
            var location = $"/api/organizations/{organizationId}/pricebook/categories/{result.Value!.Id}";
            return Results.Created(location, result.Value);
        }

        [HttpPut("{id:int}")]
        public async Task<IResult> Update(Guid organizationId, Guid id, [FromBody] PriceBookCategory body)
        {
            body.Id = id;
            body.OrganizationId = organizationId;
            var result = await _service.UpdateAsync(body);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpDelete("{id:int}")]
        public async Task<IResult> Delete(Guid organizationId, Guid id)
        {
            var result = await _service.DeleteAsync(id);
            return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
        }
    }
}