using JobFlow.Business.ConfigurationSettings;
using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.ClientModel;

namespace JobFlow.Business.Services;

[ScopedService]
public class AiWriterService : IAiWriterService
{
    private readonly OpenAiSettings _openAiSettings;

    public AiWriterService(IOptions<OpenAiSettings> openAiOptions)
    {
        _openAiSettings = openAiOptions.Value;
    }

    public async Task<Result<string>> DraftEstimateNotesAsync(Guid organizationId, string[] lineItemNames)
    {
        if (string.IsNullOrWhiteSpace(_openAiSettings.ApiKey))
            return Result.Failure<string>(Error.Failure("AiWriter.NotConfigured", "AI writer is not configured."));

        if (lineItemNames.Length == 0)
            return Result.Failure<string>(Error.Failure("AiWriter.NoLineItems", "At least one line item is required."));

        var itemList = string.Join(", ", lineItemNames.Take(20));

        var prompt = $"""
            You are a professional field service contractor writing an estimate. Given the following services/line items:

            {itemList}

            Write a short, professional estimate notes section (2-4 sentences) that:
            - Summarizes the scope of work in plain language
            - Sets expectations for how the work will be performed
            - Sounds professional and trustworthy

            Output only the notes text — no headers, no labels, no extra commentary.
            """;

        try
        {
            var client = new ChatClient(_openAiSettings.Model, _openAiSettings.ApiKey);

            var messages = new List<ChatMessage>
            {
                new UserChatMessage(prompt)
            };

            var response = await client.CompleteChatAsync(messages, new ChatCompletionOptions
            {
                MaxOutputTokenCount = 200,
                Temperature = 0.5f
            });

            var notes = response.Value.Content[0].Text.Trim();
            return Result.Success(notes);
        }
        catch (ClientResultException ex)
        {
            return Result.Failure<string>(Error.Failure("AiWriter.ApiError",
                $"The AI service returned an error (HTTP {ex.Status}). Check that your API key and model are configured correctly."));
        }
        catch (Exception)
        {
            return Result.Failure<string>(Error.Failure("AiWriter.Unavailable",
                "The AI service is temporarily unavailable. Please try again."));
        }
    }

    public async Task<Result<string>> DraftInvoiceNotesAsync(Guid organizationId, string[] lineItemDescriptions)
    {
        if (string.IsNullOrWhiteSpace(_openAiSettings.ApiKey))
            return Result.Failure<string>(Error.Failure("AiWriter.NotConfigured", "AI writer is not configured."));

        if (lineItemDescriptions.Length == 0)
            return Result.Failure<string>(Error.Failure("AiWriter.NoLineItems", "At least one line item is required."));

        var itemList = string.Join(", ", lineItemDescriptions.Take(20));

        var prompt = $"""
            You are a professional field service contractor writing an invoice. Given the following services/line items:

            {itemList}

            Write a short, professional invoice notes section (2-4 sentences) that:
            - Briefly confirms the services rendered
            - States payment terms (e.g. due within 14 days)
            - Thanks the client and invites them to reach out with questions

            Output only the notes text — no headers, no labels, no extra commentary.
            """;

        try
        {
            var client = new ChatClient(_openAiSettings.Model, _openAiSettings.ApiKey);
            var messages = new List<ChatMessage> { new UserChatMessage(prompt) };
            var response = await client.CompleteChatAsync(messages, new ChatCompletionOptions
            {
                MaxOutputTokenCount = 200,
                Temperature = 0.5f
            });
            return Result.Success(response.Value.Content[0].Text.Trim());
        }
        catch (ClientResultException ex)
        {
            return Result.Failure<string>(Error.Failure("AiWriter.ApiError",
                $"The AI service returned an error (HTTP {ex.Status}). Check that your API key and model are configured correctly."));
        }
        catch (Exception)
        {
            return Result.Failure<string>(Error.Failure("AiWriter.Unavailable",
                "The AI service is temporarily unavailable. Please try again."));
        }
    }

    public async Task<Result<string>> DraftJobSummaryAsync(Guid organizationId, string jobTitle, string[] serviceNames)
    {
        if (string.IsNullOrWhiteSpace(_openAiSettings.ApiKey))
            return Result.Failure<string>(Error.Failure("AiWriter.NotConfigured", "AI writer is not configured."));

        if (string.IsNullOrWhiteSpace(jobTitle) && serviceNames.Length == 0)
            return Result.Failure<string>(Error.Failure("AiWriter.InsufficientContext", "A job title or at least one service is required."));

        var serviceList = serviceNames.Length > 0
            ? string.Join(", ", serviceNames.Take(20))
            : "general service";

        var prompt = $"""
            You are a field service coordinator creating a job brief. The job title is "{jobTitle}" and the planned services include: {serviceList}.

            Write a short, professional job summary (2-4 sentences) that:
            - Describes what needs to be done in plain language
            - Highlights any key goals, expectations, or access notes for the crew
            - Mentions recommended scope or follow-up considerations if applicable

            Write in present/future tense as instructions for the crew — do not describe the job as completed.
            Output only the summary text — no headers, no labels, no extra commentary.
            """;

        try
        {
            var client = new ChatClient(_openAiSettings.Model, _openAiSettings.ApiKey);
            var messages = new List<ChatMessage> { new UserChatMessage(prompt) };
            var response = await client.CompleteChatAsync(messages, new ChatCompletionOptions
            {
                MaxOutputTokenCount = 200,
                Temperature = 0.5f
            });
            return Result.Success(response.Value.Content[0].Text.Trim());
        }
        catch (ClientResultException ex)
        {
            return Result.Failure<string>(Error.Failure("AiWriter.ApiError",
                $"The AI service returned an error (HTTP {ex.Status}). Check that your API key and model are configured correctly."));
        }
        catch (Exception)
        {
            return Result.Failure<string>(Error.Failure("AiWriter.Unavailable",
                "The AI service is temporarily unavailable. Please try again."));
        }
    }
}
