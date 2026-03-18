namespace JobFlow.Infrastructure.ExternalServices.Turnstile;

public sealed class TurnstileOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string ExpectedHostname { get; set; } = string.Empty;
}
