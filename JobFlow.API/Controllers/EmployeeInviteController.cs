using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeInviteController : ControllerBase
{
    private readonly IEmployeeInviteService _inviteService;

    public EmployeeInviteController(IEmployeeInviteService inviteService)
    {
        _inviteService = inviteService;
    }

    [HttpPost]
    public async Task<IResult> Invite([FromBody] EmployeeInviteDto invite)
    {
        var organizationId = HttpContext.GetOrganizationId();

        // Normalize roles: prefer RoleIds[] when present, fall back to legacy single RoleId.
        var roleIds = (invite.RoleIds is { Count: > 0 } ? invite.RoleIds : new[] { invite.RoleId })
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        var primaryRoleId = roleIds.Count > 0 ? roleIds[0] : invite.RoleId;

        var employeeInvite = new EmployeeInvite
        {
            OrganizationId = organizationId,
            Email = invite.Email,
            FirstName = invite.FirstName,
            LastName = invite.LastName,
            RoleId = primaryRoleId,
            PhoneNumber = invite.PhoneNumber,
            ExpiresAt = invite.ExpiresAt
        };

        foreach (var rid in roleIds)
        {
            employeeInvite.RoleAssignments.Add(new EmployeeInviteRoleAssignment
            {
                EmployeeRoleId = rid
            });
        }

        var result = await _inviteService.InviteAsync(employeeInvite);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("accept/{token}")]
    [AllowAnonymous]
    public async Task<IResult> AcceptInvite(Guid token)
    {
        var result = await _inviteService.AcceptInviteAsync(token);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("organization")]
    public async Task<IResult> GetByOrganization()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _inviteService.GetByOrganizationAsync(organizationId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("revoke/{inviteId:guid}")]
    public async Task<IResult> RevokeInvite(Guid inviteId)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _inviteService.RevokeAsync(inviteId, organizationId);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    [HttpGet("{code}")]
    [AllowAnonymous]
    public async Task<IResult> GetInviteByCode(string code)
    {
        var result = await _inviteService.GetInviteByCode(code);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }
}