using JobFlow.Business.Models.ConfigurationModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JobFlow.Business.ExternalServices.ReCAPTCHA
{
    public interface IReCAPTCHAService
    {
        Task<bool> VerifyTokenAsync(string token);
    }

    public class ReCAPTCHAService : IReCAPTCHAService
    {
        private readonly ReCAPTCHASettings _settings;
        private readonly HttpClient _httpClient;

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
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("score")]
        public float Score { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }
    }
}
