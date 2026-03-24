using System.Security.Claims;

namespace JobFlow.API.Extensions;

public static class HttpContextExtensions
{
    public static Guid GetOrganizationId(this HttpContext context)
    {
        var orgIdClaim = context.User.FindFirst("organizationId");

        if (orgIdClaim == null || !Guid.TryParse(orgIdClaim.Value, out var orgId))
            throw new UnauthorizedAccessException("Organization context is missing.");

        return orgId;
    }

    public static Guid GetUserId(this HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            throw new UnauthorizedAccessException("User context is missing.");

        return userId;
    }

    public static string? GetFirebaseUid(this HttpContext context)
    {
        var uid = context.User.FindFirst("user_id")?.Value;
        if (!string.IsNullOrWhiteSpace(uid))
        {
            return uid;
        }

        uid = context.User.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(uid))
        {
            return uid;
        }

        return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}