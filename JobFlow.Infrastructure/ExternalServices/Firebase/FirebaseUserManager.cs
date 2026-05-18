using FirebaseAdmin.Auth;
using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;

namespace JobFlow.Infrastructure.ExternalServices.Firebase;

/// <summary>
///     Thin adapter over <see cref="FirebaseAuth.DefaultInstance"/> so callers in
///     the business layer can manage Firebase profile data without taking a hard
///     dependency on the FirebaseAdmin SDK.
/// </summary>
[ScopedService]
public class FirebaseUserManager : IFirebaseUserManager
{
    public Task SetCustomClaimsAsync(string firebaseUid, string role, Guid organizationId)
    {
        return FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(firebaseUid,
            new Dictionary<string, object>
            {
                { "role", role },
                { "organizationId", organizationId.ToString() }
            });
    }

    public Task SetDisplayNameAsync(string firebaseUid, string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return Task.CompletedTask;

        return FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
        {
            Uid = firebaseUid,
            DisplayName = displayName
        });
    }
}
