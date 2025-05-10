using System.Net;
using System.Text;
using System.Text.Json;
using JobFlow.Business.DI;
using JobFlow.Business.Models;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Infrastructure.Common;
using JobFlow.Infrastructure.ExternalServices.ConfigurationModels;
using JobFlow.Infrastructure.HttpClients;
using Microsoft.Extensions.Options;
using Polly;

namespace JobFlow.Infrastructure.ExternalServices.Brevo
{
    [SingletonService]
    public class BrevoService : IBrevoService
    {
        private readonly BrevoSettings _settings;
        private readonly HttpClient _client;
        private readonly AsyncPolicy _policy;

        public BrevoService(
            IJobFlowHttpClientFactory httpFactory,
            IOptions<BrevoSettings> settings)
        {
            _settings = settings.Value;
            _client = httpFactory.ForBrevoClient();
            _policy = Policy.WrapAsync(
                PollyPolicies.DefaultRetryPolicy(),
                PollyPolicies.DefaultCircuitBreakerPolicy());
        }

        public async Task<bool> AddContactAsync(string email, int listId)
        {
            var payload = new
            {
                email,
                listIds = new[] { listId },
                updateEnabled = true
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = null;

            await _policy.ExecuteAsync(async () =>
            {
                response = await _client.PostAsync("contacts", content);
                response.EnsureSuccessStatusCode();
            });

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SendContactEmailAsync(ContactFormRequest request)
        {
            var payload = new
            {
                sender = new { email = "hello@katharix.com", name = "Job Flow" },
                replyTo = new { email = request.Email },
                to = new[] { new { email = request.Email } },
                templateId = request.TemplateId,
                @params = new
                {
                    Name = request.Name,
                    subject = request.Subject,
                    Email = request.Email,
                    Body = request.Message
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = null;

            await _policy.ExecuteAsync(async () =>
            {
                response = await _client.PostAsync("smtp/email", content);
                response.EnsureSuccessStatusCode();
            });

            return response.IsSuccessStatusCode;
        }
    }
}
