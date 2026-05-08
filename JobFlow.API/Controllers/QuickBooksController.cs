using System.Net.Http.Json;
using System.Text.Json;
using JobFlow.API.Extensions;
using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;
using JobFlow.Infrastructure.Integrations.QuickBooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/quickbooks")]
[Authorize]
public class QuickBooksController : ControllerBase
{
    private const string QbStatePurpose = "JobFlow.QuickBooks.OAuthState";
    private static readonly TimeSpan QbStateLifetime = TimeSpan.FromMinutes(15);

    private readonly IQuickBooksIntegrationService _qbService;
    private readonly IQuickBooksSettings _qbSettings;
    private readonly IDataProtector _stateProtector;
    private readonly IDistributedCache _distributedCache;

    public QuickBooksController(
        IQuickBooksIntegrationService qbService,
        IQuickBooksSettings qbSettings,
        IDataProtectionProvider dataProtectionProvider,
        IDistributedCache distributedCache)
    {
        _qbService = qbService;
        _qbSettings = qbSettings;
        _stateProtector = dataProtectionProvider.CreateProtector(QbStatePurpose);
        _distributedCache = distributedCache;
    }

    /// <summary>Returns the QuickBooks OAuth authorization URL. Client should redirect the browser to this URL.</summary>
    [HttpGet("connect")]
    public IActionResult Connect()
    {
        var organizationId = HttpContext.GetOrganizationId();

        if (string.IsNullOrWhiteSpace(_qbSettings.ClientId) || string.IsNullOrWhiteSpace(_qbSettings.RedirectUrl))
            return StatusCode(503, "QuickBooks OAuth is not configured.");

        var nonce = Guid.NewGuid().ToString("N");
        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var rawState = $"{organizationId:N}|{nonce}|{issuedAt}";
        var protectedState = _stateProtector.Protect(rawState);

        _distributedCache.SetString(
            GetStateCacheKey(nonce),
            "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = QbStateLifetime });

        var baseUrl = _qbSettings.UseSandbox
            ? "https://appcenter.intuit.com/connect/oauth2"
            : "https://appcenter.intuit.com/connect/oauth2";

        var authUrl = $"{baseUrl}?client_id={Uri.EscapeDataString(_qbSettings.ClientId)}" +
                      $"&response_type=code" +
                      $"&scope=com.intuit.quickbooks.accounting" +
                      $"&redirect_uri={Uri.EscapeDataString(_qbSettings.RedirectUrl)}" +
                      $"&state={Uri.EscapeDataString(protectedState)}";

        return Ok(new { authorizationUrl = authUrl });
    }

    /// <summary>OAuth callback from Intuit. Exchanges authorization code for tokens and stores the connection.</summary>
    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? realmId,
        [FromQuery] string? error = null,
        [FromQuery(Name = "error_description")] string? errorDescription = null)
    {
        var uiBase = BuildUiRedirectBaseUrl();

        if (!string.IsNullOrWhiteSpace(error))
            return Redirect($"{uiBase}?provider=quickbooks&success=false&error={Uri.EscapeDataString(errorDescription ?? error)}");

        if (string.IsNullOrWhiteSpace(code))
            return Redirect($"{uiBase}?provider=quickbooks&success=false&error={Uri.EscapeDataString("Missing authorization code.")}");

        if (string.IsNullOrWhiteSpace(realmId))
            return Redirect($"{uiBase}?provider=quickbooks&success=false&error={Uri.EscapeDataString("Missing realm ID.")}");

        if (!TryReadState(state, out var organizationId))
            return Redirect($"{uiBase}?provider=quickbooks&success=false&error={Uri.EscapeDataString("Invalid or expired OAuth state.")}");

        if (string.IsNullOrWhiteSpace(_qbSettings.ClientId)
            || string.IsNullOrWhiteSpace(_qbSettings.ClientSecret)
            || string.IsNullOrWhiteSpace(_qbSettings.RedirectUrl))
            return Redirect($"{uiBase}?provider=quickbooks&success=false&error={Uri.EscapeDataString("QuickBooks OAuth is not configured.")}");

        var credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{_qbSettings.ClientId}:{_qbSettings.ClientSecret}"));

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var body = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", _qbSettings.RedirectUrl)
        });

        var tokenResponse = await httpClient.PostAsync("https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer", body);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var failureBody = await tokenResponse.Content.ReadAsStringAsync();
            return Redirect($"{uiBase}?provider=quickbooks&success=false&error={Uri.EscapeDataString(failureBody)}");
        }

        using var doc = await JsonDocument.ParseAsync(await tokenResponse.Content.ReadAsStreamAsync());
        var root = doc.RootElement;

        var accessToken = root.TryGetProperty("access_token", out var atEl) ? atEl.GetString() : null;
        var refreshToken = root.TryGetProperty("refresh_token", out var rtEl) ? rtEl.GetString() : null;
        var expiresIn = root.TryGetProperty("expires_in", out var expEl) ? expEl.GetInt32() : 3600;
        var refreshTokenExpiresIn = root.TryGetProperty("x_refresh_token_expires_in", out var rExpEl) ? rExpEl.GetInt32() : 8640000;

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
            return Redirect($"{uiBase}?provider=quickbooks&success=false&error={Uri.EscapeDataString("Token exchange failed: tokens not returned.")}");

        var tokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn);
        var refreshExpiresAtUtc = DateTime.UtcNow.AddSeconds(refreshTokenExpiresIn);

        await _qbService.ConnectAsync(
            organizationId,
            realmId,
            accessToken,
            refreshToken,
            tokenExpiresAtUtc,
            refreshExpiresAtUtc);

        return Redirect($"{uiBase}?provider=quickbooks&success=true&realmId={Uri.EscapeDataString(realmId)}");
    }

    /// <summary>Returns the current QuickBooks connection status for the organization.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var status = await _qbService.GetStatusAsync(organizationId);
        return Ok(status);
    }

    /// <summary>Disconnects the QuickBooks integration for the organization.</summary>
    [HttpDelete("disconnect")]
    public async Task<IActionResult> Disconnect()
    {
        var organizationId = HttpContext.GetOrganizationId();
        await _qbService.DisconnectAsync(organizationId);
        return Ok();
    }

    private string BuildUiRedirectBaseUrl()
    {
        // Read FrontEndSettings base URL from config — matches how Square does it
        var baseUrl = (HttpContext.RequestServices
            .GetService<JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces.IFrontendSettings>()
            ?.BaseUrl ?? "http://localhost:4200").TrimEnd('/');
        return $"{baseUrl}/admin/settings/integrations";
    }

    private bool TryReadState(string? state, out Guid organizationId)
    {
        organizationId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(state))
            return false;

        string raw;
        try
        {
            raw = _stateProtector.Unprotect(state);
        }
        catch
        {
            return false;
        }

        var parts = raw.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            return false;

        if (!Guid.TryParse(parts[0], out organizationId))
            return false;

        var nonce = parts[1];
        if (string.IsNullOrWhiteSpace(nonce))
            return false;

        if (!long.TryParse(parts[2], out var issuedAt))
            return false;

        var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(issuedAt);
        if (age < TimeSpan.Zero || age > QbStateLifetime)
            return false;

        var cacheKey = GetStateCacheKey(nonce);
        var stateValue = _distributedCache.GetString(cacheKey);
        if (string.IsNullOrWhiteSpace(stateValue))
            return false;

        _distributedCache.Remove(cacheKey);
        return true;
    }

    private static string GetStateCacheKey(string nonce) => $"qb-oauth-state:{nonce}";
}
