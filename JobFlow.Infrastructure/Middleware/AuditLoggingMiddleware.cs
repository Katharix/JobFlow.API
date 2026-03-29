using System.Security.Claims;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Http;

namespace JobFlow.Infrastructure.Middleware;

public class AuditLoggingMiddleware
{
    private static readonly string[] SensitivePrefixes =
    {
        "/api/payments",
        "/api/auth",
        "/api/user",
        "/api/organization"
    };

    private readonly RequestDelegate _next;

    public AuditLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IAuditLogService auditLogService)
    {
        await _next(context);

        if (!ShouldLog(context))
            return;

        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;
        var status = context.Response.StatusCode;

        var request = new AuditLogWriteRequest
        {
            OrganizationId = TryGetGuidClaim(context, "organizationId"),
            UserId = TryGetGuidClaim(context, ClaimTypes.NameIdentifier),
            Category = ResolveCategory(path, status),
            Action = method,
            ResourceType = ResolveResourceType(path),
            ResourceId = context.Request.RouteValues.TryGetValue("id", out var id) ? id?.ToString() : null,
            Path = path,
            Method = method,
            StatusCode = status,
            Success = status is >= 200 and < 400,
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            DetailsJson = BuildDetails(path, status)
        };

        await auditLogService.WriteAsync(request, context.RequestAborted);
    }

    private static bool ShouldLog(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            return false;

        var status = context.Response.StatusCode;
        if (status is 401 or 403 or 429 or >= 500)
            return true;

        if (SensitivePrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            return true;

        return context.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE";
    }

    private static Guid? TryGetGuidClaim(HttpContext context, string claimType)
    {
        var value = context.User.FindFirst(claimType)?.Value;
        return Guid.TryParse(value, out var guid) ? guid : null;
    }

    private static string ResolveCategory(string path, int statusCode)
    {
        if (statusCode is 401 or 403)
            return "Authorization";

        if (path.StartsWith("/api/payments", StringComparison.OrdinalIgnoreCase))
            return "Payment";

        if (path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase))
            return "Authentication";

        return "Application";
    }

    private static string ResolveResourceType(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 ? segments[1] : "Unknown";
    }

    private static string BuildDetails(string path, int statusCode)
    {
        var details = new Dictionary<string, object?>
        {
            ["path"] = path,
            ["statusCode"] = statusCode,
            ["recordedAtUtc"] = DateTime.UtcNow
        };

        return System.Text.Json.JsonSerializer.Serialize(details);
    }
}
