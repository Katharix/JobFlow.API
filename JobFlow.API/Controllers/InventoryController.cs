using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using JobFlow.Business.Extensions;

namespace JobFlow.API.Controllers
{
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
        public async Task<IResult> Get(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpGet("org/{orgId}")]
        public async Task<IResult> GetAll(Guid orgId)
        {
            var result = await _service.GetAllAsync(orgId);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpPost]
        public async Task<IResult> Create([FromBody] InventoryItem item)
        {
            var result = await _service.CreateAsync(item);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpPut]
        public async Task<IResult> Update([FromBody] InventoryItem item)
        {
            var result = await _service.UpdateAsync(item);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpDelete("{id}")]
        public async Task<IResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
        }
    }
}
