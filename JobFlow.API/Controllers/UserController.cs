using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Business.Extensions;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace JobFlow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [Authorize]
        [HttpGet]
        public async Task<IResult> GetAll()
        {
            var result = await _userService.GetAllUsers();
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IResult> GetById(Guid id)
        {
            var result = await _userService.GetUserById(id);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpPost]
        public async Task<IResult> CreateOrUpdate([FromBody] User model)
        {
            var result = await _userService.UpsertUser(model);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IResult> Delete(Guid id)
        {
            var result = await _userService.DeleteUser(id);
            return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
        }

        [HttpPost("{id}/assign-role")]
        public async Task<IResult> AssignRole(Guid id, [FromQuery] string role)
        {
            var result = await _userService.AssignRole(id, role);
            return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
        }

        [Authorize]
        [HttpGet("firebase/{uid}")]
        public async Task<IResult> GetByFirebaseUid(string uid)
        {
            var result = await _userService.GetUserByFirebaseUid(uid);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }
    }
}
