using System.Net.Http.Json;
using System.Text.Json;
using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobFlow.Infrastructure.PaymentGateways.Square;

public interface ISquareTokenRefreshService
{
    Task<SquareTokenSet?> RefreshIfNeededAsync(Guid organizationId);
}

public record SquareTokenSet(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);

[ScopedService]
public class SquareTokenRefreshService : ISquareTokenRefreshService
{
    private readonly IPaymentProfileService _paymentProfileService;
    private readonly ISquareTokenEncryptionService _encryption;
    private readonly ISquareSettings _settings;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<SquareTokenRefreshService> _logger;

    public SquareTokenRefreshService(
        IPaymentProfileService paymentProfileService,
        ISquareTokenEncryptionService encryption,
        ISquareSettings settings,
        IHostEnvironment hostEnvironment,
        ILogger<SquareTokenRefreshService> logger)
    {
        _paymentProfileService = paymentProfileService;
        _encryption = encryption;
        _settings = settings;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task<SquareTokenSet?> RefreshIfNeededAsync(Guid organizationId)
    {
        var profileResult = await _paymentProfileService.GetForOrganizationAsync(organizationId, PaymentProvider.Square);
        if (profileResult.IsFailure)
            return null;

        var profile = profileResult.Value;
        if (string.IsNullOrWhiteSpace(profile.EncryptedAccessToken))
            return null;

        var accessToken = _encryption.Decrypt(profile.EncryptedAccessToken);

        // If token is still valid for > 5 minutes, return as-is
        if (profile.TokenExpiresAtUtc.HasValue && profile.TokenExpiresAtUtc.Value > DateTime.UtcNow.AddMinutes(5))
        {
            return new SquareTokenSet(
                accessToken,
                string.IsNullOrWhiteSpace(profile.EncryptedRefreshToken)
                    ? string.Empty
                    : _encryption.Decrypt(profile.EncryptedRefreshToken),
                profile.TokenExpiresAtUtc.Value);
        }

        // Attempt refresh
        if (string.IsNullOrWhiteSpace(profile.EncryptedRefreshToken))
        {
            _logger.LogWarning("Square token expired for org {OrgId} and no refresh token available", organizationId);
            return null;
        }

        var refreshToken = _encryption.Decrypt(profile.EncryptedRefreshToken);

        var connectBaseUrl = _hostEnvironment.IsDevelopment()
            ? "https://connect.squareupsandbox.com"
            : "https://connect.squareup.com";

        using var httpClient = new HttpClient();
        var response = await httpClient.PostAsJsonAsync($"{connectBaseUrl}/oauth2/token", new
        {
            client_id = _settings.ApplicationId,
            client_secret = _settings.ClientSecret,
            grant_type = "refresh_token",
            refresh_token = refreshToken
        });

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Square token refresh failed for org {OrgId}: {Body}", organizationId, body);
            return null;
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;

        var newAccessToken = root.TryGetProperty("access_token", out var atEl) ? atEl.GetString() : null;
        var newRefreshToken = root.TryGetProperty("refresh_token", out var rtEl) ? rtEl.GetString() : null;
        var expiresAt = root.TryGetProperty("expires_at", out var expEl) ? expEl.GetString() : null;

        if (string.IsNullOrWhiteSpace(newAccessToken))
        {
            _logger.LogError("Square token refresh returned empty access_token for org {OrgId}", organizationId);
            return null;
        }

        var expiresAtUtc = !string.IsNullOrWhiteSpace(expiresAt)
            ? DateTime.Parse(expiresAt).ToUniversalTime()
            : DateTime.UtcNow.AddDays(30);

        await _paymentProfileService.UpdateTokensAsync(
            profile.Id,
            _encryption.Encrypt(newAccessToken),
            string.IsNullOrWhiteSpace(newRefreshToken) ? profile.EncryptedRefreshToken : _encryption.Encrypt(newRefreshToken),
            expiresAtUtc);

        _logger.LogInformation("Square token refreshed for org {OrgId}, expires {ExpiresAt}", organizationId, expiresAtUtc);

        return new SquareTokenSet(newAccessToken, newRefreshToken ?? refreshToken, expiresAtUtc);
    }
}
