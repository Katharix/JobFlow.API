using JobFlow.Business.ExternalServices.Brevo.Models;
using JobFlow.Business.Models.ConfigurationModels;
using JobFlow.Infrastructure.HttpClients;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JobFlow.Business.ExternalServices.Brevo
{
    public class BrevoService : IBrevoService
    {
        private readonly BrevoSettings _settings;
        private readonly HttpClient _client;

        public BrevoService(IJobFlowHttpClientFactory httpFactory, IOptions<BrevoSettings> settings)
        {
            _settings = settings.Value;
            _client = httpFactory.ForBrevoClient();
            _client.DefaultRequestHeaders.Add("api-key", _settings.ApiKey);
        }

        public async Task<bool> AddContactAsync(string email, int listId)
        {
            var payload = new
            {
                email = email,
                listIds = new[] { listId },
                updateEnabled = true
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync("contacts", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SendContactEmailAsync(ContactFormRequest request)
        {
            var payload = new
            {
                sender = new { email = "hello@katharix.com", name = "Katharix Contact Form" },
                replyTo = new { email = request.Email },
                to = new[]
                {
            new { email = "hello@katharix.com", name = "Katharix Contact Form" }
        },
                subject = "New Contact Form Message",
                htmlContent = $@"
            <h3>New message from {request.Name} ({request.Email})</h3>
            <p>{System.Net.WebUtility.HtmlEncode(request.Message)}</p>"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync("smtp/email", content);
            return response.IsSuccessStatusCode;
        }

    }

}
