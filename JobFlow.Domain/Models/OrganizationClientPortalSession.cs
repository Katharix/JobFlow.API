using System.Security.Cryptography;

namespace JobFlow.Domain.Models;

public class OrganizationClientPortalSession : Entity
{
    public Guid OrganizationId { get; set; }
    public Guid OrganizationClientId { get; set; }

    public string EmailAddress { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RedeemedAt { get; set; }

    public OrganizationClient? OrganizationClient { get; set; }

    public bool IsExpired => ExpiresAt <= DateTimeOffset.UtcNow;
    public bool IsRedeemed => RedeemedAt.HasValue;

    public static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
