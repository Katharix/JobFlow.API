using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace JobFlow.Infrastructure.ExternalServices.Turnstile;

public sealed class TurnstileVerificationService : ICaptchaVerificationService
{
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
    private readonly HttpClient _httpClient;
    private readonly TurnstileOptions _options;

    public TurnstileVerificationService(HttpClient httpClient, IOptions<TurnstileOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<CaptchaVerificationResult> VerifyAsync(
        string token,
        string expectedAction,
        string? remoteIp,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            return new CaptchaVerificationResult
            {
                IsValid = false,
                ErrorCodes = ["missing-input"]
            };
        }

        var form = new Dictionary<string, string>
        {
            ["secret"] = _options.SecretKey,
            ["response"] = token
        };

        if (!string.IsNullOrWhiteSpace(remoteIp))
            form["remoteip"] = remoteIp;

        using var content = new FormUrlEncodedContent(form);
        using var response = await _httpClient.PostAsync(VerifyUrl, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new CaptchaVerificationResult
            {
                IsValid = false,
                ErrorCodes = ["verification-http-failure"]
            };
        }

        var payload = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>(cancellationToken: cancellationToken);

        if (payload is null || !payload.Success)
        {
            return new CaptchaVerificationResult
            {
                IsValid = false,
                ErrorCodes = payload?.ErrorCodes ?? Array.Empty<string>(),
                Action = payload?.Action,
                Hostname = payload?.Hostname
            };
        }

        if (!string.Equals(payload.Action, expectedAction, StringComparison.Ordinal))
        {
            return new CaptchaVerificationResult
            {
                IsValid = false,
                ErrorCodes = ["action-mismatch"],
                Action = payload.Action,
                Hostname = payload.Hostname
            };
        }

        if (!string.IsNullOrWhiteSpace(_options.ExpectedHostname) &&
            !string.Equals(payload.Hostname, _options.ExpectedHostname, StringComparison.OrdinalIgnoreCase))
        {
            return new CaptchaVerificationResult
            {
                IsValid = false,
                ErrorCodes = ["hostname-mismatch"],
                Action = payload.Action,
                Hostname = payload.Hostname
            };
        }

        return new CaptchaVerificationResult
        {
            IsValid = true,
            ErrorCodes = payload.ErrorCodes ?? Array.Empty<string>(),
            Action = payload.Action,
            Hostname = payload.Hostname
        };
    }

    private sealed class TurnstileVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }
    }
}
