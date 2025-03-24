using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;

namespace JobFlow.Infrastructure.Middleware
{
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
                    new Claim(ClaimTypes.NameIdentifier, decodedToken.Uid)
                };

                    if (decodedToken.Claims.ContainsKey("role"))
                    {
                        claims.Add(new Claim(ClaimTypes.Role, decodedToken.Claims["role"].ToString()));
                    }

                    var identity = new ClaimsIdentity(claims, "Firebase");
                    context.User = new ClaimsPrincipal(identity);
                }
                catch
                {
                    context.Response.StatusCode = 401;
                    return;
                }
            }

            await _next(context);
        }
    }
}
