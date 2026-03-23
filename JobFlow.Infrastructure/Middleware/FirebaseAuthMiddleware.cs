using System.Security.Claims;
using FirebaseAdmin.Auth;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace JobFlow.Infrastructure.Middleware;

public class FirebaseAuthMiddleware
{
    private readonly RequestDelegate _next;

    public FirebaseAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IUserService userService)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value?.ToLower();

        // Skip endpoints that must be reachable without a Firebase bearer token.
        if (path != null &&
           (path.StartsWith("/api/organizations/register") ||
            path.StartsWith("/api/organizations/retrieve") ||
            path.StartsWith("/api/organization/types") ||
            path.StartsWith("/api/auth/") ||
            path.StartsWith("/api/client-hub-auth") ||
            path.StartsWith("/api/client-hub")))
        {
            await _next(context);
            return;
        }

        string? token = null;

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            token = authHeader.Substring("Bearer ".Length);
        }

        if (string.IsNullOrWhiteSpace(token)
            && path is not null
            && path.StartsWith("/hubs/")
            && context.Request.Query.TryGetValue("access_token", out var accessToken))
        {
            token = accessToken.FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            await _next(context);
            return;
        }

        // If this is a locally-issued Client Portal JWT, do not attempt Firebase verification.
        // The JwtBearer handler for the ClientPortalJwt scheme will validate and populate claims.
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwt = handler.ReadJwtToken(token);
                if (string.Equals(jwt.Issuer, "JobFlow.ClientPortal", StringComparison.Ordinal))
                {
                    await _next(context);
                    return;
                }
            }
        }
        catch
        {
            // ignore and fall back to Firebase verification
        }

        try
        {
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, decodedToken.Uid)
        };

            if (decodedToken.Claims.TryGetValue("role", out var role))
            {
                var roleValue = Convert.ToString(role);
                if (!string.IsNullOrWhiteSpace(roleValue))
                    claims.Add(new Claim(ClaimTypes.Role, roleValue));
            }

            if (decodedToken.Claims.TryGetValue("email", out var email))
            {
                var emailValue = Convert.ToString(email);
                if (!string.IsNullOrWhiteSpace(emailValue))
                    claims.Add(new Claim(ClaimTypes.Email, emailValue));
            }

            var userResult = await userService.GetUserByFirebaseUid(decodedToken.Uid);

            if (!userResult.IsSuccess)
            {
                if (context.Response.HasStarted)
                    return;

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("User is not linked to an organization.");
                return;
            }

            claims.Add(new Claim("organizationId", userResult.Value.OrganizationId.ToString()));

            var identity = new ClaimsIdentity(claims, "Firebase");
            context.User.AddIdentity(identity);
        }
        catch
        {
            if (context.Response.HasStarted)
                return;

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context);
    }
}
