using System.Security.Claims;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Http;

namespace JobFlow.Infrastructure.Middleware;

public class FirebaseAuthMiddleware
{
    private readonly RequestDelegate _next;

    public FirebaseAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length);

            try
            {
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, decodedToken.Uid)
                };

                if (decodedToken.Claims.TryGetValue("role", out var role))
                    claims.Add(new Claim(ClaimTypes.Role, role.ToString()));

                if (decodedToken.Claims.TryGetValue("email", out var email))
                    claims.Add(new Claim(ClaimTypes.Email, email.ToString()));

                var identity = new ClaimsIdentity(claims, "Firebase");
                context.User = new ClaimsPrincipal(identity);
            }
            catch
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await _next(context);
    }
}