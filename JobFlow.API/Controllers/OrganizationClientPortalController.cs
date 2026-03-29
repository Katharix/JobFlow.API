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
        var result = await _portal.RedeemMagicLinkAsync(request.Token);

        if (!result.IsSuccess)
            return result.ToProblemDetails();

        var client = result.Value;
        var (accessToken, expiresAt) = IssueClientPortalJwt(client.OrganizationId, client.Id);

        return Results.Ok(new
        {
            accessToken,
            tokenType = "Bearer",
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

    private (string token, DateTimeOffset expiresAt) IssueClientPortalJwt(Guid organizationId, Guid organizationClientId)
    {
        var signingKey = _configuration["Auth:ClientPortal:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
            throw new InvalidOperationException("Missing configuration: Auth:ClientPortal:SigningKey");

        var expiresAt = DateTimeOffset.UtcNow.AddHours(8);

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
