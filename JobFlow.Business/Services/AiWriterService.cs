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
}
