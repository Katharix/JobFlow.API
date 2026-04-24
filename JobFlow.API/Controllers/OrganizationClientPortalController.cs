using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/client-portal")]
public class OrganizationClientPortalController : ControllerBase
{
    private readonly IOrganizationClientPortalService _portal;
    private readonly IConfiguration _configuration;

    public OrganizationClientPortalController(IOrganizationClientPortalService portal, IConfiguration configuration)
    {
        _portal = portal;
        _configuration = configuration;
    }

    [HttpPost("send-link/{organizationClientId:guid}")]
    [Authorize(Policy = "OrganizationEmployeeOnly")]
    public async Task<IResult> SendLink(Guid organizationClientId, [FromBody] SendMagicLinkRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _portal.SendMagicLinkAsync(organizationId, organizationClientId, request.EmailAddress);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    [HttpPost("redeem")]
    [AllowAnonymous]
    public async Task<IResult> Redeem([FromBody] RedeemMagicLinkRequest request)
    {
        if (!TryGetClientPortalSigningKey(out var signingKey, out var configurationError))
            return configurationError!;

        var result = await _portal.RedeemMagicLinkAsync(request.Token);

        if (!result.IsSuccess)
            return result.ToProblemDetails();

        var client = result.Value;
        var (accessToken, expiresAt) = IssueClientPortalJwt(client.OrganizationId, client.Id, signingKey);

        HttpContext.Response.Cookies.Append("jobflow.clientHub.token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt,
            Path = "/"
        });

        return Results.Ok(new
        {
            expiresAt,
            client = new
            {
                id = client.Id,
                firstName = client.FirstName,
                lastName = client.LastName,
                emailAddress = client.EmailAddress
            }
        });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IResult Logout()
    {
        HttpContext.Response.Cookies.Delete("jobflow.clientHub.token", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });

        return Results.Ok();
    }

    private bool TryGetClientPortalSigningKey(out string signingKey, out IResult? errorResult)
    {
        signingKey = _configuration["Auth:ClientPortal:SigningKey"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(signingKey))
            signingKey = _configuration["Auth-ClientPortal-SigningKey"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            errorResult = Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Client portal authentication is temporarily unavailable.",
                detail: "Client portal auth is not configured. Please contact support.");
            return false;
        }

        errorResult = null;
        return true;
    }

    private (string token, DateTimeOffset expiresAt) IssueClientPortalJwt(Guid organizationId, Guid organizationClientId, string signingKey)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(2);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, organizationClientId.ToString()),
            new(ClaimTypes.Role, UserRoles.OrganizationClient),
            new("organizationId", organizationId.ToString())
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: "JobFlow.ClientPortal",
            audience: "JobFlow.ClientPortal",
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expiresAt);
    }
}

public record SendMagicLinkRequest(string EmailAddress);
public record RedeemMagicLinkRequest(string Token);
