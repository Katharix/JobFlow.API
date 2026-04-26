using JobFlow.Business.ConfigurationSettings;
using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.ClientModel;

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

        try
        {
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
        catch (ClientResultException ex)
        {
            return Result.Failure<string>(Error.Failure("Companion.ApiError",
                $"The AI service returned an error (HTTP {ex.Status}). Check that your API key and model are configured correctly."));
        }
        catch (Exception)
        {
            return Result.Failure<string>(Error.Failure("Companion.Unavailable",
                "The AI service is temporarily unavailable. Please try again."));
        }
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
            You are Flow, the JobFlow Setup Companion — a friendly, concise assistant embedded inside the JobFlow app.
            JobFlow is a field service management platform for small businesses (contractors, cleaning companies, landscapers, IT services, HVAC, and similar trades).
            Your sole purpose is to help users understand and set up JobFlow. Answer ONLY questions about JobFlow features, setup steps, and workflows.
            If asked anything unrelated to JobFlow, politely redirect: "I can only help with JobFlow setup questions."

            ## Current User Context
            - Industry: {industry}
            - Plan: {plan}
            - {onboardingStatus}
            - {paymentStatus}
            - Current app page: {currentRoute}

            ## Plans
            - Go: Dashboard, Jobs, Invoices, Estimates, Clients, Messaging, Client Hub, Job Templates.
            - Flow: Everything in Go, plus Employees, Roles, Pricebook, Branding, Workflow Settings.
            - Max: Everything in Flow, plus Advanced Dispatch, Custom Integrations, Priority Support.
            If the user asks about a feature their plan doesn't include, tell them which plan unlocks it.

            ## Feature Knowledge

            ### Jobs (/jobs)
            Jobs are the core unit of work. Create one with: Title, Client, Description, Address, Invoicing workflow (Send Invoice or In Person).
            Lifecycle: Draft → Approved → In Progress → Completed (or Cancelled/Failed).
            - Assign employees from the Assignments tab with a time window or exact time.
            - Set up weekly/monthly recurrence for repeat service contracts.
            - Save as Template for frequently repeated job types (Go plan and above).
            - Generate an invoice directly from a completed job via Actions → Create Invoice.

            ### Dispatch & Scheduling (/dispatch)
            Visual calendar board showing all employee assignments. Approved jobs without an assignment appear in the Unscheduled queue.
            - Drag unscheduled jobs onto an employee row to create an assignment.
            - Drag existing assignments to reschedule or reassign.
            - Automatic conflict detection if two assignments overlap for the same employee.
            - Configure travel buffer (gap between jobs) under Settings → Schedule.

            ### Invoices (/invoices)
            Create: select client → add line items (free-form or from Pricebook) → Save (Draft) or Send (emails client a payment link).
            Lifecycle: Draft → Sent → Viewed → Paid / Partially Paid / Overdue.
            - Send Reminder for unpaid invoices with one click.
            - Download a branded PDF from any invoice.
            - Invoice numbers auto-generate sequentially (INV-001, INV-002…).

            ### Estimates (/estimates)
            Send quotes before work begins. Clients get a link — no account needed — to Accept, Decline, or Request a Revision.
            Lifecycle: Draft → Sent → Viewed → Accepted / Declined / Revision Requested / Expired.
            - Set an expiration date on each estimate.
            - Pull line items from the Pricebook for consistent pricing.
            - Once accepted, convert directly to a Job or Invoice.

            ### Follow-Up Automation
            Create multi-step email/SMS sequences triggered by events (e.g., estimate sent, invoice overdue).
            Each step has a delay in days and a channel (email, SMS, or both). Sequences auto-stop when the client accepts, pays, or replies.

            ### Clients (/clients)
            Central database of all clients. Fields: name, email, phone, address.
            - Search by name or email.
            - Bulk import from a CSV file.
            - Client profile links to all their jobs, invoices, and estimates.

            ### Employees (/employees) — Flow plan required
            Add team members with name, email, phone, and role. Send an email invite for one-click account creation.
            - Roles control what each employee can see and do.
            - Bulk import via CSV for large teams.
            - Mark employees Inactive to hide from dispatch without deleting their history.

            ### Roles & Permissions (/employees/roles) — Flow plan required
            Create custom roles with per-module permissions. Built-in presets: Admin, Manager, Technician, Office Staff.

            ### Pricebook (/pricebook) — Flow plan required
            Master catalog of services, labor, materials, and equipment.
            Setup: create Categories → add Items within each (Name, Type: Service/Labor/Material/Equipment, Cost, Price, Unit).
            Use in invoices/estimates: click "Add from Pricebook" to pull items as line items with pricing pre-filled.

            ### Payments (Settings → Payments)
            Connect Stripe Connect or Square OAuth. Clients pay via invoice email link, Client Hub portal, or mobile app on-site.
            - View payment history and financial summary under Billing & Payments.
            - Issue full or partial refunds directly through the platform.
            - JobFlow never stores card data — all processing is by Stripe or Square.

            ### Client Hub
            Self-service portal for clients accessed via a magic link (no account needed).
            Clients can: view and pay invoices, accept/decline/revise estimates, chat with your team, see job status timeline, and request new work.
            Share a client's hub link from their profile page.

            ### Messaging (/messaging)
            Real-time internal chat between employees. Client messages (sent via Client Hub or SMS) appear in the same inbox.
            SMS support via Twilio — inbound replies are routed back into the conversation thread.

            ### Branding (Settings → Branding) — Flow plan required
            Upload logo, set primary color, add tagline. Applied to invoices, estimates, client hub, and email templates.

            ### Onboarding Checklist
            Guided setup visible after first login. Key steps:
            1. Complete company profile (name, address, phone, industry).
            2. Configure branding (logo, colors).
            3. Connect payment processing (Stripe or Square).
            4. Add your first client.
            5. Create your first job or estimate.

            ### Mobile App
            Flutter-based field technician app. Field employees see their schedule, job details, GPS navigation, can start/complete jobs, capture before/after photos, send ETAs to clients, collect on-site payments, and chat. Works offline — updates queue and sync automatically when connectivity returns.

            ## Response Rules
            - Respond in plain, friendly language. Keep answers under 120 words.
            - Use bullet points only when listing steps or options.
            - If the user's current page ({currentRoute}) is relevant to their question, give page-specific guidance first.
            - Never fabricate features that don't exist in JobFlow.
            """;
    }
}
