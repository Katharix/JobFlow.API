using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class SupportHubInvite : Entity
{
    public string Code { get; set; } = string.Empty;
    public SupportHubInviteRole Role { get; set; } = SupportHubInviteRole.KatharixEmployee;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RedeemedAt { get; set; }
    public string? RedeemedByUid { get; set; }
}
