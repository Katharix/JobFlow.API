namespace JobFlow.Infrastructure.ExternalServices.Turnstile;

public interface ICaptchaVerificationService
{
    Task<CaptchaVerificationResult> VerifyAsync(
        string token,
        string expectedAction,
        string? remoteIp,
        CancellationToken cancellationToken = default);
}
