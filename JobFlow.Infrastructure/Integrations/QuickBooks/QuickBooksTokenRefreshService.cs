using System.Net.Http.Json;
using System.Text.Json;
using JobFlow.Business.DI;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Infrastructure.Integrations.QuickBooks;

public interface IQuickBooksTokenRefreshService
{
    Task<QuickBooksTokenSet?> RefreshIfNeededAsync(Guid organizationId);
}

public record QuickBooksTokenSet(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);

[ScopedService]
public class QuickBooksTokenRefreshService : IQuickBooksTokenRefreshService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQuickBooksTokenEncryptionService _encryption;
    private readonly IQuickBooksSettings _settings;
    private readonly ILogger<QuickBooksTokenRefreshService> _logger;

    public QuickBooksTokenRefreshService(
        IUnitOfWork unitOfWork,
        IQuickBooksTokenEncryptionService encryption,
        IQuickBooksSettings settings,
        ILogger<QuickBooksTokenRefreshService> logger)
    {
        _unitOfWork = unitOfWork;
        _encryption = encryption;
        _settings = settings;
        _logger = logger;
    }

    public async Task<QuickBooksTokenSet?> RefreshIfNeededAsync(Guid organizationId)
    {
        var connection = await _unitOfWork.RepositoryOf<QuickBooksConnection>()
            .Query()
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.IsConnected);

        if (connection is null || string.IsNullOrWhiteSpace(connection.EncryptedAccessToken))
            return null;

        var accessToken = _encryption.Decrypt(connection.EncryptedAccessToken);

        // If token is still valid for > 5 minutes, return as-is
        if (connection.TokenExpiresAtUtc.HasValue && connection.TokenExpiresAtUtc.Value > DateTime.UtcNow.AddMinutes(5))
        {
            return new QuickBooksTokenSet(
                accessToken,
                string.IsNullOrWhiteSpace(connection.EncryptedRefreshToken)
                    ? string.Empty
                    : _encryption.Decrypt(connection.EncryptedRefreshToken),
                connection.TokenExpiresAtUtc.Value);
        }

        // Attempt refresh
        if (string.IsNullOrWhiteSpace(connection.EncryptedRefreshToken))
        {
            _logger.LogWarning("QuickBooks token expired for org {OrgId} and no refresh token available", organizationId);
            return null;
        }

        var refreshToken = _encryption.Decrypt(connection.EncryptedRefreshToken);

        var tokenEndpoint = _settings.UseSandbox
            ? "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer"
            : "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";

        var credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var body = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var response = await httpClient.PostAsync(tokenEndpoint, body);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("QuickBooks token refresh failed for org {OrgId}: {Body}", organizationId, errorBody);
            return null;
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;

        var newAccessToken = root.TryGetProperty("access_token", out var atEl) ? atEl.GetString() : null;
        var newRefreshToken = root.TryGetProperty("refresh_token", out var rtEl) ? rtEl.GetString() : null;
        var expiresIn = root.TryGetProperty("expires_in", out var expEl) ? expEl.GetInt32() : 3600;
        var xRefreshTokenExpiresIn = root.TryGetProperty("x_refresh_token_expires_in", out var rExpEl) ? rExpEl.GetInt32() : 8640000;

        if (string.IsNullOrWhiteSpace(newAccessToken))
        {
            _logger.LogError("QuickBooks token refresh returned empty access_token for org {OrgId}", organizationId);
            return null;
        }

        var newExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn);
        var newRefreshExpiresAtUtc = DateTime.UtcNow.AddSeconds(xRefreshTokenExpiresIn);

        connection.EncryptedAccessToken = _encryption.Encrypt(newAccessToken);
        connection.EncryptedRefreshToken = string.IsNullOrWhiteSpace(newRefreshToken)
            ? connection.EncryptedRefreshToken
            : _encryption.Encrypt(newRefreshToken);
        connection.TokenExpiresAtUtc = newExpiresAtUtc;
        connection.RefreshTokenExpiresAtUtc = newRefreshExpiresAtUtc;

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("QuickBooks token refreshed for org {OrgId}, expires {ExpiresAt}", organizationId, newExpiresAtUtc);

        return new QuickBooksTokenSet(
            newAccessToken,
            newRefreshToken ?? refreshToken,
            newExpiresAtUtc);
    }
}
