using System.Text.Json;
using System.Text.Json.Serialization;
using JobFlow.Business.DI;
using JobFlow.Infrastructure.ExternalServices.ConfigurationModels;
using Microsoft.Extensions.Options;

namespace JobFlow.Infrastructure.ExternalServices.ReCAPTCHA;

public interface IReCAPTCHAService
{
    Task<bool> VerifyTokenAsync(string token);
}

[ScopedService]
public class ReCAPTCHAService : IReCAPTCHAService
{
    private readonly HttpClient _httpClient;
    private readonly ReCAPTCHASettings _settings;

    public ReCAPTCHAService(IOptions<ReCAPTCHASettings> settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<bool> VerifyTokenAsync(string token)
    {
        var values = new Dictionary<string, string>
        {
            { "secret", _settings.SecretKey },
            { "response", token }
        };

        var content = new FormUrlEncodedContent(values);

        var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RecaptchaResponse>(json);

        return result?.Success ?? false;
    }
}

public class RecaptchaResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }

    [JsonPropertyName("score")] public float Score { get; set; }

    [JsonPropertyName("action")] public string? Action { get; set; }
}