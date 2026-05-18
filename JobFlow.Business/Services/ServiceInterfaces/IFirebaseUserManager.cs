namespace JobFlow.Business.Services.ServiceInterfaces;

/// <summary>
///     Abstraction over the Firebase Admin SDK so business-layer code can manage
///     user profile / custom claims without depending on the Infrastructure layer.
/// </summary>
public interface IFirebaseUserManager
{
    /// <summary>Set the JobFlow role + organization claims on a Firebase account.</summary>
    Task SetCustomClaimsAsync(string firebaseUid, string role, Guid organizationId);

    /// <summary>Update the display name on a Firebase account. No-op when display name is blank.</summary>
    Task SetDisplayNameAsync(string firebaseUid, string? displayName);
}
