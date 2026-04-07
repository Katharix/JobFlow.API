using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

if (args.Length < 3)
{
    Console.WriteLine("Usage: SetFirebaseClaims <path-to-firebase-adminsdk.json> <firebase-uid> <role>");
    Console.WriteLine("Example: SetFirebaseClaims ../../../JobFlow.API/job-flow-firebase-adminsdk.json qUjHtdJaeWYdEApz0immwdfTGmI2 KatharixAdmin");
    return 1;
}

var credentialPath = args[0];
var uid = args[1];
var role = args[2];

FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(credentialPath)
});

var claims = new Dictionary<string, object> { { "role", role } };
await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(uid, claims);

// Verify
var user = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
Console.WriteLine($"Set claims for {user.Email} (UID: {uid})");
Console.WriteLine($"Custom claims: {System.Text.Json.JsonSerializer.Serialize(user.CustomClaims)}");
return 0;
