namespace JobFlow.Domain.Models;

public class QuickBooksConnection : Entity
{
    public Guid OrganizationId { get; set; }

    /// <summary>QuickBooks company realm ID (returned by Intuit OAuth).</summary>
    public string RealmId { get; set; } = string.Empty;

    /// <summary>Encrypted QBO access token.</summary>
    public string? EncryptedAccessToken { get; set; }

    /// <summary>Encrypted QBO refresh token.</summary>
    public string? EncryptedRefreshToken { get; set; }

    /// <summary>When the access token expires (UTC). QBO tokens expire after 1 hour; refresh tokens after 100 days.</summary>
    public DateTime? TokenExpiresAtUtc { get; set; }

    /// <summary>When the refresh token expires (UTC).</summary>
    public DateTime? RefreshTokenExpiresAtUtc { get; set; }

    /// <summary>Whether the connection is currently active.</summary>
    public bool IsConnected { get; set; } = true;

    /// <summary>Timestamp of the last successful sync (UTC).</summary>
    public DateTime? LastSyncedAtUtc { get; set; }
}
