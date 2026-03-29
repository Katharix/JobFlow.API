using FirebaseAdmin.Auth;
using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/supporthub")]
public class SupportHubController : ControllerBase
{
    private readonly ISupportHubInviteService _inviteService;
    private readonly ISupportHubService _supportHubService;
    private readonly IUserService _userService;
    private readonly IOrganizationService _organizationService;

    public SupportHubController(
        ISupportHubService supportHubService,
        ISupportHubInviteService inviteService,
        IUserService userService,
        IOrganizationService organizationService)
    {
        _supportHubService = supportHubService;
        _inviteService = inviteService;
        _userService = userService;
        _organizationService = organizationService;
    }

    [HttpPost("register")]
    [Authorize]
    public async Task<IResult> RegisterSupportHubUser()
    {
        var firebaseUid = HttpContext.GetFirebaseUid();
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            return Results.Unauthorized();
        }

        var orgResult = await _organizationService.GetAllOrganizations();
        if (orgResult.IsFailure)
        {
            return orgResult.ToProblemDetails();
        }

        var masterOrg = orgResult.Value
            .FirstOrDefault(o => o.OrganizationType?.TypeName == "Master Account");
        if (masterOrg == null)
        {
            return Results.Problem("Master account organization not found.");
        }

        var userResult = await _userService.GetUserByFirebaseUid(firebaseUid);
        if (userResult.IsFailure)
        {
            var email = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var newUser = new User
            {
                Email = email,
                FirebaseUid = firebaseUid,
                OrganizationId = masterOrg.Id
            };

            var createResult = await _userService.UpsertUser(newUser);
            if (createResult.IsFailure)
            {
                return createResult.ToProblemDetails();
            }

            await _userService.AssignRole(createResult.Value.Id, UserRoles.KatharixEmployee);
        }
        else
        {
            var existingUser = userResult.Value;
            if (existingUser.OrganizationId == Guid.Empty)
            {
                existingUser.OrganizationId = masterOrg.Id;
                var updateResult = await _userService.UpsertUser(existingUser);
                if (updateResult.IsFailure)
                {
                    return updateResult.ToProblemDetails();
                }
            }

            await _userService.AssignRole(existingUser.Id, UserRoles.KatharixEmployee);
        }

        await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(
            firebaseUid,
            new Dictionary<string, object>
            {
                { "role", UserRoles.KatharixEmployee }
            });

        return Results.Ok();
    }

    [HttpGet("tickets")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> GetTickets()
    {
        var result = await _supportHubService.GetTicketsAsync();
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("sessions")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> GetSessions()
    {
        var result = await _supportHubService.GetSessionsAsync();
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("sessions/{sessionId:guid}/screen")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> StartScreenView([FromRoute] Guid sessionId)
    {
        var result = await _supportHubService.CreateScreenViewAsync(sessionId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("tickets")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> CreateTicket([FromBody] SupportHubTicketCreateRequest request)
    {
        var createdBy = HttpContext.GetFirebaseUid();
        var result = await _supportHubService.CreateTicketAsync(request, createdBy);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("sessions")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> CreateSession([FromBody] SupportHubSessionCreateRequest request)
    {
        var createdBy = HttpContext.GetFirebaseUid();
        var result = await _supportHubService.CreateSessionAsync(request, createdBy);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("seed")]
    [Authorize(Roles = UserRoles.KatharixAdmin)]
    public async Task<IResult> SeedDemo([FromBody] SupportHubSeedRequest request)
    {
        var createdBy = HttpContext.GetFirebaseUid();
        var result = await _supportHubService.SeedDemoAsync(request, createdBy);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("invites")]
    [Authorize(Roles = UserRoles.KatharixAdmin)]
    public async Task<IResult> GetInvites()
    {
        var result = await _inviteService.GetActiveInvitesAsync();
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("invites")]
    [Authorize(Roles = UserRoles.KatharixAdmin)]
    public async Task<IResult> CreateInvite([FromBody] SupportHubInviteCreateRequest request)
    {
        var createdBy = HttpContext.GetFirebaseUid();
        var result = await _inviteService.CreateInviteAsync(request, createdBy);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("invites/validate/{code}")]
    [AllowAnonymous]
    public async Task<IResult> ValidateInvite([FromRoute] string code)
    {
        var result = await _inviteService.ValidateInviteAsync(code);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("invites/redeem")]
    [Authorize]
    public async Task<IResult> RedeemInvite([FromBody] SupportHubInviteRedeemRequest request)
    {
        var firebaseUid = HttpContext.GetFirebaseUid();
        var result = await _inviteService.RedeemInviteAsync(request.Code, firebaseUid);
        if (result.IsFailure)
        {
            return result.ToProblemDetails();
        }

        if (!string.IsNullOrWhiteSpace(firebaseUid))
        {
            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(
                firebaseUid,
                new Dictionary<string, object>
                {
                    { "role", result.Value.Role.ToString() }
                });
        }

        return Results.Ok(result.Value);
    }
}
