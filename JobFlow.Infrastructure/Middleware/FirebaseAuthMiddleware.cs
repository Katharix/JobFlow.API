using System.Security.Claims;
using FirebaseAdmin.Auth;
using JobFlow.Business.Services.ServiceInterfaces;
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
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length);

            try
            {
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, decodedToken.Uid)
                };

                if (decodedToken.Claims.TryGetValue("role", out var role))
                    claims.Add(new Claim(ClaimTypes.Role, role.ToString()));

                if (decodedToken.Claims.TryGetValue("email", out var email))
                    claims.Add(new Claim(ClaimTypes.Email, email.ToString()));

                // Resolve the application user/org context.
                // If this fails, do not continue the pipeline with a partial identity.
                var userResult = await userService.GetUserByFirebaseUid(decodedToken.Uid);
                if (!userResult.IsSuccess)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("User is not linked to an organization.");
                    return;
                }

                claims.Add(new Claim(
                    "organizationId",
                    userResult.Value.OrganizationId.ToString()
                ));

                // Augment the current principal instead of replacing it.
                // This avoids losing claims set by the ASP.NET Core authentication handler.
                var identity = new ClaimsIdentity(claims, authenticationType: "Firebase");

                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    context.User.AddIdentity(identity);
                }
                else
                {
                    context.User = new ClaimsPrincipal(identity);
                }
            }
            catch (Exception ex)
            { 
                var test = ex.Message;
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await _next(context);
    }

}
