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
[Route("api/client-hub-auth")]
public class ClientHubAuthController : ControllerBase
{
    private readonly IOrganizationClientPortalService _portal;
    private readonly IOrganizationClientService _clients;
    private readonly IConfiguration _configuration;

    public ClientHubAuthController(
        IOrganizationClientPortalService portal,
        IOrganizationClientService clients,
        IConfiguration configuration)
    {
        _portal = portal;
        _clients = clients;
        _configuration = configuration;
    }

    [HttpPost("magic-link/request")]
    [AllowAnonymous]
    public async Task<IResult> RequestMagicLink([FromBody] ClientHubMagicLinkRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EmailAddress))
            return Results.BadRequest("EmailAddress is required.");

        // Anonymous endpoint: resolve OrganizationClients by email.
        // If not found, return 200 to avoid account enumeration.
        var matchesResult = await _clients.GetOrganizationClientsByEmailAsync(request.EmailAddress);
        if (!matchesResult.IsSuccess)
            return Results.Ok();

        var matches = matchesResult.Value;

        // If caller didn't disambiguate and there are multiple matches, return options.
        if (!request.OrganizationClientId.HasValue && matches.Count > 1)
        {
            return Results.Ok(new
            {
                requiresOrganizationSelection = true,
                clients = matches.Select(c => new
                {
                    id = c.Id,
                    organizationId = c.OrganizationId,
                    organizationName = c.Organization?.OrganizationName,
                    firstName = c.FirstName,
                    lastName = c.LastName,
                    emailAddress = c.EmailAddress
                })
            });
        }

        var target = request.OrganizationClientId.HasValue
            ? matches.FirstOrDefault(c => c.Id == request.OrganizationClientId.Value)
            : matches.FirstOrDefault();

        if (target is null)
            return Results.Ok();

        var result = await _portal.SendMagicLinkAsync(target.OrganizationId, target.Id, target.EmailAddress ?? request.EmailAddress);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    [HttpPost("magic-link/redeem")]
    [AllowAnonymous]
    public async Task<IResult> RedeemMagicLink([FromBody] ClientHubRedeemMagicLinkRequest request)
    {
        if (!TryGetClientPortalSigningKey(out var signingKey, out var configurationError))
            return configurationError!;

        var result = await _portal.RedeemMagicLinkAsync(request.Token);
        if (!result.IsSuccess)
            return result.ToProblemDetails();

        var client = result.Value;
        var (accessToken, expiresAt) = IssueClientPortalJwt(client.OrganizationId, client.Id, signingKey);

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

    private (string accessToken, DateTimeOffset expiresAt) IssueClientPortalJwt(Guid organizationId, Guid organizationClientId, string signingKey)
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

public record ClientHubMagicLinkRequest(string EmailAddress, Guid? OrganizationClientId = null);
public record ClientHubRedeemMagicLinkRequest(string Token);
