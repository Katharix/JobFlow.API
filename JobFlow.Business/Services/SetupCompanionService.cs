using JobFlow.Business.ConfigurationSettings;
using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace JobFlow.Business.Services;

[ScopedService]
public class SetupCompanionService : ISetupCompanionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly OpenAiSettings _openAiSettings;

    public SetupCompanionService(IUnitOfWork unitOfWork, IOptions<OpenAiSettings> openAiOptions)
    {
        _unitOfWork = unitOfWork;
        _openAiSettings = openAiOptions.Value;
    }

    public async Task<Result> TrackEventAsync(Guid organizationId, string sessionId, string questionKey, string? answerKey)
    {
        var ev = new SetupCompanionEvent
        {
            OrganizationId = organizationId,
            SessionId = sessionId,
            QuestionKey = questionKey,
            AnswerKey = answerKey,
            OccurredAt = DateTimeOffset.UtcNow
        };

        await _unitOfWork.RepositoryOf<SetupCompanionEvent>().AddAsync(ev);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<string>> AskAsync(Guid organizationId, string sessionId, string question, string currentRoute)
    {
        if (string.IsNullOrWhiteSpace(_openAiSettings.ApiKey))
            return Result.Failure<string>(Error.Failure("Companion.NotConfigured", "AI companion is not configured."));

        var org = await _unitOfWork.RepositoryOf<Organization>().GetByIdAsync(organizationId);
        if (org is null)
            return Result.Failure<string>(Error.NotFound("Organization.NotFound", "Organization not found."));

        // Track the free-text ask as an analytics event
        var ev = new SetupCompanionEvent
        {
            OrganizationId = organizationId,
            SessionId = sessionId,
            QuestionKey = "free-text",
            AnswerKey = null,
            OccurredAt = DateTimeOffset.UtcNow
        };
        await _unitOfWork.RepositoryOf<SetupCompanionEvent>().AddAsync(ev);
        await _unitOfWork.SaveChangesAsync();

        var systemPrompt = BuildSystemPrompt(org, currentRoute);

        var client = new ChatClient(_openAiSettings.Model, _openAiSettings.ApiKey);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(question)
        };

        var response = await client.CompleteChatAsync(messages, new ChatCompletionOptions
        {
            MaxOutputTokenCount = 400,
            Temperature = 0.3f
        });

        var answer = response.Value.Content[0].Text;
        return Result.Success(answer);
    }

    private static string BuildSystemPrompt(Organization org, string currentRoute)
    {
        var onboardingStatus = org.OnBoardingComplete
            ? "Onboarding complete."
            : "Onboarding not yet complete.";

        var paymentStatus = org.CanAcceptPayments
            ? "Payment processing is connected."
            : org.PaymentSetupDeferred
                ? "Payment setup is deferred — not yet connected."
                : "Payment processing is not connected.";

        var industry = string.IsNullOrWhiteSpace(org.IndustryKey) ? "general" : org.IndustryKey;
        var plan = string.IsNullOrWhiteSpace(org.SubscriptionPlanName) ? "Go" : org.SubscriptionPlanName;

        return $"""
            You are the JobFlow Setup Companion — a friendly, concise assistant embedded inside the JobFlow app.
            JobFlow is a field service management platform for small businesses (contractors, cleaning companies, landscapers, IT services, HVAC, and similar trades).

            Your sole purpose is to help users understand and set up JobFlow. You answer ONLY questions about:
            - How JobFlow features work (jobs, estimates, invoices, scheduling, clients, employees, pricebook, payments, branding)
            - What the user should do next in their setup
            - Whether a step is required or optional for them

            If asked anything unrelated to JobFlow, politely redirect: "I can only help with JobFlow setup questions."

            Current user context:
            - Industry: {industry}
            - Plan: {plan}
            - {onboardingStatus}
            - {paymentStatus}
            - Current page in the app: {currentRoute}

            Respond in plain, friendly language. Keep answers under 100 words. Use bullet points only when listing steps.
            Never fabricate features that don't exist in JobFlow.
            """;
    }
}
