using JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("i")]
public class InviteRedirectController : ControllerBase
{
    private readonly IFrontendSettings _frontendSettings;
    private readonly IEmployeeInviteService _invites;

    public InviteRedirectController(IEmployeeInviteService invites, IFrontendSettings frontendSettings)
    {
        _invites = invites;
        _frontendSettings = frontendSettings;
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> RedirectInvite(string code)
    {
        var invite = await _invites.GetInviteByCode(code);

        if (invite is null)
            return NotFound("Invite not found or expired.");
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var redirectResult = await _invites.ResolveShortCodeAsync(code, ipAddress);
        if (string.IsNullOrEmpty(redirectResult.Value))
            return NotFound("No invite redirect url.");

        return Redirect(redirectResult.Value);
    }
}