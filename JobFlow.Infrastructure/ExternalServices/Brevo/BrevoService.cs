using System.Collections.Generic;
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

namespace JobFlow.Infrastructure.ExternalServices.Brevo;

internal static class BrevoListIds
{
    public const int Newsletter = 3;
    public const int TrialUsers = 4;
}

[SingletonService]
public class BrevoService : IBrevoService
{
    private readonly HttpClient _client;
    private readonly AsyncPolicy _policy;
    private readonly BrevoSettings _settings;

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

        HttpResponseMessage? response = null;

        await _policy.ExecuteAsync(async () =>
        {
            response = await _client.PostAsync("contacts", content);
            response.EnsureSuccessStatusCode();
        });

        return response is not null && response.IsSuccessStatusCode;
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
                request.Name,
                subject = request.Subject,
                request.Email,
                Body = request.Message,
                InvoiceLink = request.Link
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage? response = null;

        await _policy.ExecuteAsync(async () =>
        {
            response = await _client.PostAsync("smtp/email", content);
            response.EnsureSuccessStatusCode();
        });

        return response is not null && response.IsSuccessStatusCode;
    }

    public async Task<bool> AddTrialContactAsync(string email, string firstName, string lastName, string orgName, DateTimeOffset trialStartDate)
    {
        var payload = new
        {
            email,
            listIds = new[] { BrevoListIds.TrialUsers },
            updateEnabled = true,
            attributes = new Dictionary<string, object>
            {
                ["FIRSTNAME"] = firstName,
                ["LASTNAME"] = lastName,
                ["ORG_NAME"] = orgName,
                ["TRIAL_START_DATE"] = trialStartDate.ToString("yyyy-MM-dd")
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage? response = null;

        await _policy.ExecuteAsync(async () =>
        {
            response = await _client.PostAsync("contacts", content);
            response.EnsureSuccessStatusCode();
        });

        return response is not null && response.IsSuccessStatusCode;
    }

    public async Task TrackActivationEventAsync(string email, string eventKey)
    {
        var payload = new
        {
            attributes = new Dictionary<string, object> { [eventKey] = true }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        await _policy.ExecuteAsync(async () =>
        {
            var response = await _client.PutAsync($"contacts/{Uri.EscapeDataString(email)}", content);
            response.EnsureSuccessStatusCode();
        });
    }
}