using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("l")]
[AllowAnonymous]
public class ShortLinkRedirectController : ControllerBase
{
    private readonly IShortLinkService _shortLinkService;

    public ShortLinkRedirectController(IShortLinkService shortLinkService)
    {
        _shortLinkService = shortLinkService;
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> Redirect(string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 16)
            return BadRequest();

        var targetUrl = await _shortLinkService.ResolveAsync(code, cancellationToken);

        if (targetUrl is null)
            return NotFound("Link not found or expired.");

        return Redirect(targetUrl);
    }
}
