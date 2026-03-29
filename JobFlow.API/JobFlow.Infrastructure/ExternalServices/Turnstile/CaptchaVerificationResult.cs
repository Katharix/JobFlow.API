namespace JobFlow.Infrastructure.ExternalServices.Turnstile;

public sealed class CaptchaVerificationResult
{
    public bool IsValid { get; init; }
    public string[] ErrorCodes { get; init; } = Array.Empty<string>();
    public string? Action { get; init; }
    public string? Hostname { get; init; }
}
