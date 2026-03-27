using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class CustomerPaymentProfile : Entity
{
    public Guid OwnerId { get; set; }
    public PaymentEntityType OwnerType { get; set; }
    public PaymentProvider Provider { get; set; }
    public string ProviderCustomerId { get; set; } = string.Empty;
    public string? DefaultPaymentMethodId { get; set; }
    public bool IsDelinquent { get; set; } = false;

    /// <summary>Square OAuth access token (encrypted at rest).</summary>
    public string? EncryptedAccessToken { get; set; }

    /// <summary>Square OAuth refresh token (encrypted at rest).</summary>
    public string? EncryptedRefreshToken { get; set; }

    /// <summary>When the current access token expires (UTC).</summary>
    public DateTime? TokenExpiresAtUtc { get; set; }

    /// <summary>Square location id chosen during onboarding.</summary>
    public string? SquareLocationId { get; set; }

    public Guid? OrganizationClientId { get; set; }
    public Guid? OrganizationId { get; set; }
}