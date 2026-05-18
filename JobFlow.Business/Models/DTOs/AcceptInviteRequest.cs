namespace JobFlow.Business.Models.DTOs;

/// <summary>
///     Payload sent by an invitee to finalize their invitation. The Firebase user
///     is created client-side (so passwords never cross our API), and the caller
///     passes the resulting UID + display name so the backend can link a User
///     and Employee record to the new identity.
/// </summary>
public class AcceptInviteRequest
{
    /// <summary>UID returned by Firebase Auth after the client created the account.</summary>
    public string FirebaseUid { get; set; } = string.Empty;

    /// <summary>Display first name. Falls back to the value captured on the invite.</summary>
    public string? FirstName { get; set; }

    /// <summary>Display last name. Falls back to the value captured on the invite.</summary>
    public string? LastName { get; set; }
}
