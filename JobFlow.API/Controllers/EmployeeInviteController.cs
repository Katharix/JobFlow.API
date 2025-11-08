using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeInviteController : ControllerBase
    {
        private readonly IEmployeeInviteService _inviteService;

        public EmployeeInviteController(IEmployeeInviteService inviteService)
        {
            this._inviteService = inviteService;
        }

        [HttpPost]
        public async Task<IResult> Invite([FromBody] EmployeeInviteDto invite)
        {
            var employeeInvite = new EmployeeInvite
            {
                OrganizationId = invite.OrganizationId,
                Email = invite.Email,
                FirstName = invite.FirstName,
                LastName = invite.LastName,
                RoleId = invite.RoleId,
                PhoneNumber = invite.PhoneNumber,
                ExpiresAt = invite.ExpiresAt
            };
            var result = await _inviteService.InviteAsync(employeeInvite);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpPost("accept/{token}")]
        public async Task<IResult> AcceptInvite(string token)
        {
            var result = await _inviteService.AcceptInviteAsync(token);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpGet("{code}")]
        public async Task<IResult> GetInviteByCode(string code)
        {
            var result = await _inviteService.GetInviteByCode(code);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

    }
}
