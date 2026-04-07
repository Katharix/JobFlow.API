using JobFlow.Business.DI;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Business.Services;

[ScopedService]
public class HelpContentService : IHelpContentService
{
    private readonly IRepository<HelpArticle> _articles;
    private readonly IRepository<ChangelogEntry> _changelog;
    private readonly IUnitOfWork _unitOfWork;

    public HelpContentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _articles = unitOfWork.RepositoryOf<HelpArticle>();
        _changelog = unitOfWork.RepositoryOf<ChangelogEntry>();
    }

    // ── Articles ──────────────────────────────────────────

    public async Task<Result<List<HelpArticleDto>>> GetPublishedArticlesAsync()
    {
        var articles = await _articles.Query()
            .AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderBy(x => x.Category)
            .ThenBy(x => x.SortOrder)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => ToArticleDto(x))
            .ToListAsync();

        return Result.Success(articles);
    }

    public async Task<Result<List<HelpArticleDto>>> GetAllArticlesAsync()
    {
        var articles = await _articles.Query()
            .AsNoTracking()
            .OrderBy(x => x.Category)
            .ThenBy(x => x.SortOrder)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => ToArticleDto(x))
            .ToListAsync();

        return Result.Success(articles);
    }

    public async Task<Result<HelpArticleDto>> GetArticleByIdAsync(Guid id)
    {
        var article = await _articles.Query()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => ToArticleDto(x))
            .FirstOrDefaultAsync();

        if (article is null)
            return Result.Failure<HelpArticleDto>(
                Error.NotFound("HelpContent.ArticleNotFound", "Article not found."));

        return Result.Success(article);
    }

    public async Task<Result<HelpArticleDto>> CreateArticleAsync(
        HelpArticleCreateRequest request, string? createdBy)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<HelpArticleDto>(
                Error.Validation("HelpContent.TitleRequired", "Title is required."));

        if (request.Title.Length > 200)
            return Result.Failure<HelpArticleDto>(
                Error.Validation("HelpContent.TitleTooLong", "Title must be 200 characters or fewer."));

        if (string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure<HelpArticleDto>(
                Error.Validation("HelpContent.ContentRequired", "Content is required."));

        var article = new HelpArticle
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Summary = request.Summary?.Trim(),
            Content = request.Content,
            ArticleType = request.ArticleType,
            Category = request.Category,
            Tags = request.Tags?.Trim(),
            IsFeatured = request.IsFeatured,
            IsPublished = request.IsPublished,
            SortOrder = request.SortOrder,
            PublishedBy = request.IsPublished ? createdBy : null,
            PublishedAt = request.IsPublished ? DateTimeOffset.UtcNow : null,
            CreatedBy = createdBy
        };

        await _articles.AddAsync(article);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(ToArticleDto(article));
    }

    public async Task<Result<HelpArticleDto>> UpdateArticleAsync(
        HelpArticleUpdateRequest request, string? updatedBy)
    {
        var article = await _articles.FirstOrDefaultAsync(x => x.Id == request.Id);
        if (article is null)
            return Result.Failure<HelpArticleDto>(
                Error.NotFound("HelpContent.ArticleNotFound", "Article not found."));

        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<HelpArticleDto>(
                Error.Validation("HelpContent.TitleRequired", "Title is required."));

        if (request.Title.Length > 200)
            return Result.Failure<HelpArticleDto>(
                Error.Validation("HelpContent.TitleTooLong", "Title must be 200 characters or fewer."));

        if (string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure<HelpArticleDto>(
                Error.Validation("HelpContent.ContentRequired", "Content is required."));

        var wasPublished = article.IsPublished;

        article.Title = request.Title.Trim();
        article.Summary = request.Summary?.Trim();
        article.Content = request.Content;
        article.ArticleType = request.ArticleType;
        article.Category = request.Category;
        article.Tags = request.Tags?.Trim();
        article.IsFeatured = request.IsFeatured;
        article.IsPublished = request.IsPublished;
        article.SortOrder = request.SortOrder;
        article.UpdatedBy = updatedBy;
        article.UpdatedAt = DateTime.UtcNow;

        if (!wasPublished && request.IsPublished)
        {
            article.PublishedBy = updatedBy;
            article.PublishedAt = DateTimeOffset.UtcNow;
        }

        await _articles.UpdateAsync(article);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(ToArticleDto(article));
    }

    public async Task<Result> DeleteArticleAsync(Guid id)
    {
        var article = await _articles.FirstOrDefaultAsync(x => x.Id == id);
        if (article is null)
            return Result.Failure(
                Error.NotFound("HelpContent.ArticleNotFound", "Article not found."));

        await _articles.RemoveAsync(article);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    // ── Changelog ─────────────────────────────────────────

    public async Task<Result<List<ChangelogEntryDto>>> GetPublishedChangelogAsync()
    {
        var entries = await _changelog.Query()
            .AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => ToChangelogDto(x))
            .ToListAsync();

        return Result.Success(entries);
    }

    public async Task<Result<List<ChangelogEntryDto>>> GetAllChangelogAsync()
    {
        var entries = await _changelog.Query()
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToChangelogDto(x))
            .ToListAsync();

        return Result.Success(entries);
    }

    public async Task<Result<ChangelogEntryDto>> CreateChangelogEntryAsync(
        ChangelogEntryCreateRequest request, string? createdBy)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<ChangelogEntryDto>(
                Error.Validation("HelpContent.TitleRequired", "Title is required."));

        if (request.Title.Length > 200)
            return Result.Failure<ChangelogEntryDto>(
                Error.Validation("HelpContent.TitleTooLong", "Title must be 200 characters or fewer."));

        var entry = new ChangelogEntry
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Version = request.Version?.Trim(),
            Category = request.Category,
            IsPublished = request.IsPublished,
            PublishedAt = request.IsPublished ? DateTimeOffset.UtcNow : null,
            CreatedBy = createdBy
        };

        await _changelog.AddAsync(entry);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(ToChangelogDto(entry));
    }

    public async Task<Result<ChangelogEntryDto>> UpdateChangelogEntryAsync(
        ChangelogEntryUpdateRequest request, string? updatedBy)
    {
        var entry = await _changelog.FirstOrDefaultAsync(x => x.Id == request.Id);
        if (entry is null)
            return Result.Failure<ChangelogEntryDto>(
                Error.NotFound("HelpContent.ChangelogNotFound", "Changelog entry not found."));

        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<ChangelogEntryDto>(
                Error.Validation("HelpContent.TitleRequired", "Title is required."));

        var wasPublished = entry.IsPublished;

        entry.Title = request.Title.Trim();
        entry.Description = request.Description?.Trim();
        entry.Version = request.Version?.Trim();
        entry.Category = request.Category;
        entry.IsPublished = request.IsPublished;
        entry.UpdatedBy = updatedBy;
        entry.UpdatedAt = DateTime.UtcNow;

        if (!wasPublished && request.IsPublished)
            entry.PublishedAt = DateTimeOffset.UtcNow;

        await _changelog.UpdateAsync(entry);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(ToChangelogDto(entry));
    }

    public async Task<Result> DeleteChangelogEntryAsync(Guid id)
    {
        var entry = await _changelog.FirstOrDefaultAsync(x => x.Id == id);
        if (entry is null)
            return Result.Failure(
                Error.NotFound("HelpContent.ChangelogNotFound", "Changelog entry not found."));

        await _changelog.RemoveAsync(entry);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    // ── Mapping helpers ───────────────────────────────────

    private static HelpArticleDto ToArticleDto(HelpArticle x) => new(
        x.Id,
        x.Title,
        x.Summary,
        x.Content,
        x.ArticleType,
        x.Category,
        x.Tags,
        x.IsFeatured,
        x.IsPublished,
        x.SortOrder,
        x.PublishedAt,
        new DateTimeOffset(x.CreatedAt, TimeSpan.Zero));

    private static ChangelogEntryDto ToChangelogDto(ChangelogEntry x) => new(
        x.Id,
        x.Title,
        x.Description,
        x.Version,
        x.Category,
        x.IsPublished,
        x.PublishedAt,
        new DateTimeOffset(x.CreatedAt, TimeSpan.Zero));

    // ── Seed ──────────────────────────────────────────────

    public async Task<Result> SeedHelpContentAsync(string? createdBy)
    {
        var existingArticles = await _articles.Query().AnyAsync();
        var existingChangelog = await _changelog.Query().AnyAsync();

        if (existingArticles || existingChangelog)
            return Result.Failure(Error.Conflict("HelpContent.AlreadySeeded", "Help content has already been seeded."));

        var now = DateTimeOffset.UtcNow;

        var articles = new List<HelpArticle>
        {
            // ── Getting Started ─────────────────────────────
            new()
            {
                Id = Guid.NewGuid(), Title = "Welcome to JobFlow",
                Summary = "A quick overview of what JobFlow does and how it helps you run your service business.",
                Content = "JobFlow is an all-in-one platform for service businesses — contractors, designers, consultants, tech repair shops, and more. From a single dashboard you can manage jobs, send estimates and invoices, schedule your team, message clients, and accept payments.\n\nAfter signing up you'll be guided through onboarding, which includes setting up your company profile, connecting a payment provider (Stripe or Square), and optionally inviting employees.\n\nYour main workspace is the Admin Dashboard. From there you can access Jobs, Clients, Invoicing, Estimates, Dispatch, Messaging, and Settings. Clients you add can view their own portal — the Client Hub — where they track estimates, invoices, job progress, and chat with your team.\n\nYour subscription plan determines which features are available. The Go plan covers essentials, while the Flow plan unlocks branding, workflow customization, employee roles, and the price book.",
                ArticleType = HelpArticleType.Guide, Category = HelpArticleCategory.GettingStarted,
                Tags = "onboarding,overview,getting started", IsFeatured = true, IsPublished = true, SortOrder = 1,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Completing Onboarding",
                Summary = "Step-by-step walkthrough of the onboarding checklist.",
                Content = "When you first sign in, JobFlow presents an onboarding checklist to help you get set up quickly.\n\n1. **Company Profile** — Enter your business name, address, phone number, and default tax rate under Settings → Company.\n2. **Connect Payments** — Link your Stripe or Square account so you can accept online payments through invoices.\n3. **Add Your First Client** — Head to Clients and create a client record with name, email, and address.\n4. **Create a Job** — Go to Jobs → New Job, assign a client, set the schedule, and add any notes.\n5. **Invite Employees** (Flow plan) — Under Employees, generate invite links so your team can join.\n\nEach step is tracked on the onboarding page. Once all steps are complete the checklist disappears automatically.",
                ArticleType = HelpArticleType.Guide, Category = HelpArticleCategory.GettingStarted,
                Tags = "onboarding,setup,checklist", IsFeatured = false, IsPublished = true, SortOrder = 2,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "What subscription plans are available?",
                Summary = "Understand the differences between the Go and Flow plans.",
                Content = "JobFlow offers two subscription tiers:\n\n**Go Plan** — Includes jobs, clients, invoicing, estimates, dispatch, messaging, and payment processing. Best for solo operators or small teams who need the essentials.\n\n**Flow Plan** — Everything in Go, plus branding customization, workflow settings, employee role management, and the price book. Best for growing teams that want a professional, branded experience.\n\nYou can upgrade or manage your subscription at any time from Settings → Subscription Management.",
                ArticleType = HelpArticleType.Faq, Category = HelpArticleCategory.GettingStarted,
                Tags = "plans,pricing,subscription", IsFeatured = false, IsPublished = true, SortOrder = 3,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },

            // ── Jobs ────────────────────────────────────────
            new()
            {
                Id = Guid.NewGuid(), Title = "Creating and Managing Jobs",
                Summary = "Learn how to create jobs, assign employees, and track progress.",
                Content = "Jobs are the core of JobFlow. Each job tracks a piece of work from start to finish.\n\n**Creating a job:**\n1. Go to Jobs → New Job.\n2. Enter a title and optional description.\n3. Select or create a client.\n4. Choose the invoicing workflow — \"Send invoice after completion\" or \"Collect payment in person.\"\n5. Save the job.\n\n**Job statuses:** Draft → Approved → In Progress → Completed. You can also mark a job as Cancelled or Failed.\n\n**Assignments:** Within a job you can create one or more assignments. Each assignment has its own schedule (exact time or window), address, notes, and assigned employees. The lead assignee is the primary responsible person.\n\n**Recurring jobs:** Toggle recurrence on to repeat a job Weekly or Monthly. Configure the interval, day, and end condition (never, on a date, or after a count).\n\n**Job updates:** Add notes, photos, and status changes to keep a running log of work performed.",
                ArticleType = HelpArticleType.Guide, Category = HelpArticleCategory.Jobs,
                Tags = "jobs,assignments,scheduling,recurring", IsFeatured = true, IsPublished = true, SortOrder = 1,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "What are job assignments?",
                Summary = "Understand how assignments break a job into scheduled work blocks.",
                Content = "An assignment represents a single scheduled visit or task within a job. Large jobs might have multiple assignments — for example, an initial consultation and a follow-up installation.\n\nEach assignment has:\n- **Schedule type** — Window (a time range) or Exact (a specific start time).\n- **Status** — Scheduled, In Progress, Completed, Skipped, or Canceled.\n- **Address** — The location where the work is performed.\n- **Assignees** — One or more employees. Mark one as the lead.\n- **Notes** — Internal instructions for the team.\n\nAssignments appear on the Dispatch board and the Scheduling view so your team always knows where to be.",
                ArticleType = HelpArticleType.Faq, Category = HelpArticleCategory.Jobs,
                Tags = "assignments,scheduling", IsFeatured = false, IsPublished = true, SortOrder = 2,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },

            // ── Invoicing ───────────────────────────────────
            new()
            {
                Id = Guid.NewGuid(), Title = "Creating and Sending Invoices",
                Summary = "How to create invoices, add line items, and send them to clients.",
                Content = "Invoices let you bill clients for completed work.\n\n**Creating an invoice:**\n1. Go to Invoices → New Invoice, or generate one directly from a completed job.\n2. Select the client and optionally link the invoice to a job.\n3. Add line items — each item has a description, quantity, and unit price.\n4. Review the subtotal, tax, and total.\n5. Save as Draft or Send immediately.\n\n**Invoice statuses:** Draft → Sent → Paid. Invoices can also be marked Overdue or Unpaid.\n\n**Payments:** When a client opens the invoice link, they can pay online via Stripe or Square (depending on your connected account). The amount paid, balance due, and payment date are tracked automatically.\n\n**Client Hub:** Clients can view and pay invoices from their Client Hub portal without needing a password — they use a secure magic link.",
                ArticleType = HelpArticleType.Guide, Category = HelpArticleCategory.Invoicing,
                Tags = "invoices,billing,payments,line items", IsFeatured = true, IsPublished = true, SortOrder = 1,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "How do clients pay invoices?",
                Summary = "Learn about the client payment experience.",
                Content = "When you send an invoice, the client receives a link to view it. From there they can pay online using a credit card or other payment method supported by your connected Stripe or Square account.\n\nThe payment flow is:\n1. Client opens the invoice link.\n2. They review the line items and total.\n3. They click Pay and enter payment details.\n4. Once processed, the invoice status updates to Paid and you receive a notification.\n\nIf you chose \"Collect payment in person\" during job creation, the invoice is marked as paid manually after you collect payment on-site.",
                ArticleType = HelpArticleType.Faq, Category = HelpArticleCategory.Invoicing,
                Tags = "payments,clients,stripe,square", IsFeatured = false, IsPublished = true, SortOrder = 2,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },

            // ── Estimates ───────────────────────────────────
            new()
            {
                Id = Guid.NewGuid(), Title = "Creating and Sending Estimates",
                Summary = "Build estimates with line items and send them for client approval.",
                Content = "Estimates let you quote a price before starting work.\n\n**Creating an estimate:**\n1. Go to Estimates → New Estimate.\n2. Select or create a client.\n3. Add line items with name, description, quantity, and unit price.\n4. Add any notes or terms.\n5. Save as Draft.\n\n**Sending:** Click Send, enter the recipient email, and optionally include a personal message. The client receives a link to view the estimate.\n\n**Estimate statuses:** Draft → Sent → Accepted or Declined. Estimates can also be Cancelled or Expired.\n\n**Revisions:** If the client requests changes, the estimate moves to Revision Requested. You review the request, make edits, and resolve or reject the revision.\n\n**Converting to a job:** Once an estimate is accepted, create a job directly from it to carry over the client and line-item details.",
                ArticleType = HelpArticleType.Guide, Category = HelpArticleCategory.Estimates,
                Tags = "estimates,quotes,line items,revisions", IsFeatured = true, IsPublished = true, SortOrder = 1,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Can clients request changes to an estimate?",
                Summary = "How the estimate revision workflow works.",
                Content = "Yes. When a client views an estimate in the Client Hub, they can request a revision instead of accepting or declining.\n\nThe revision workflow:\n1. **Requested** — The client submits a revision request with comments.\n2. **In Review** — You receive the request and review it.\n3. **Resolved** — You update the estimate and mark the revision as resolved, then re-send.\n4. **Rejected** — If the request isn't feasible, you can reject it with an explanation.\n\nClients can also attach files to their revision requests. You'll see all revision history on the estimate detail page.",
                ArticleType = HelpArticleType.Faq, Category = HelpArticleCategory.Estimates,
                Tags = "estimates,revisions,client hub", IsFeatured = false, IsPublished = true, SortOrder = 2,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },

            // ── Clients ─────────────────────────────────────
            new()
            {
                Id = Guid.NewGuid(), Title = "Managing Clients",
                Summary = "How to add, edit, and organize your client records.",
                Content = "Clients are the people and businesses you do work for.\n\n**Adding a client:**\n1. Go to Clients → Add Client.\n2. Enter the client's first name, last name, email, phone, and address.\n3. Save.\n\nClients are automatically linked to your organization. You can associate them with jobs, invoices, and estimates.\n\n**Client Hub access:** Each client can access their own Client Hub portal using a secure magic link — no password required. From there they can view estimates, pay invoices, track job progress, request work, and chat with your team.\n\n**Editing:** Click any client to update their details. Changes are reflected everywhere the client appears.",
                ArticleType = HelpArticleType.Guide, Category = HelpArticleCategory.Clients,
                Tags = "clients,contacts,client hub", IsFeatured = false, IsPublished = true, SortOrder = 1,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "What can clients do in the Client Hub?",
                Summary = "Overview of the client-facing portal features.",
                Content = "The Client Hub is a self-service portal for your clients. They access it via a magic link — no login or password required.\n\nFrom the Client Hub, clients can:\n- **View estimates** and accept, decline, or request revisions.\n- **View and pay invoices** online using Stripe or Square.\n- **Track jobs** — see upcoming assignments, status updates, notes, and photos.\n- **Chat** with your business in real time.\n- **Request work** — submit a new service request with a subject, description, preferred date, and budget.\n- **Update their profile** — name, email, phone, and address.\n\nThe Client Hub uses your branding (logo, colors) if you're on the Flow plan.",
                ArticleType = HelpArticleType.Faq, Category = HelpArticleCategory.Clients,
                Tags = "client hub,portal,self-service", IsFeatured = false, IsPublished = true, SortOrder = 2,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },

            // ── Employees ───────────────────────────────────
            new()
            {
                Id = Guid.NewGuid(), Title = "Inviting and Managing Employees",
                Summary = "Add team members, assign roles, and manage permissions.",
                Content = "Employees are the team members who help run your business. This feature is available on the Flow plan.\n\n**Inviting employees:**\n1. Go to Employees → Invite.\n2. An invite link is generated. Share it with your team member.\n3. When they sign up using the link, they're automatically added to your organization.\n\n**Roles:** Each employee has a role that determines their permissions.\n- **Organization Admin** — Full access to all features including settings and billing.\n- **Organization Employee** — Access to jobs, scheduling, clients, and messaging. Cannot change settings or billing.\n\nYou can customize roles at Employees → Roles, or start from one of the preset templates.\n\n**Managing employees:** View all team members, their contact info, role, and active status. Deactivate an employee to revoke their access without deleting their records.",
                ArticleType = HelpArticleType.Guide, Category = HelpArticleCategory.Employees,
                Tags = "employees,team,roles,invites,flow plan", IsFeatured = false, IsPublished = true, SortOrder = 1,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },

            // ── Dispatch ────────────────────────────────────
            new()
            {
                Id = Guid.NewGuid(), Title = "Using the Dispatch Board",
                Summary = "Visually assign and schedule jobs across your team.",
                Content = "The Dispatch board gives you a visual timeline of your team's assignments.\n\n**How it works:**\n- The left column lists your employees.\n- The main area shows each employee's scheduled assignments on a date-based timeline.\n- Unscheduled jobs appear in a sidebar.\n\n**Assigning work:** Drag an unscheduled job from the sidebar and drop it onto an employee's timeline to create an assignment. Set the time, address, and any notes.\n\n**Rescheduling:** Drag an assignment to a different time or employee to reschedule it. If \"Auto-notify on reschedule\" is enabled in Schedule Settings, affected clients and employees are notified automatically.\n\n**Travel buffer:** Enable travel buffer in Schedule Settings to ensure there's enough time between back-to-back assignments. Set the default buffer in minutes.",
                ArticleType = HelpArticleType.Guide, Category = HelpArticleCategory.Dispatch,
                Tags = "dispatch,scheduling,drag and drop,timeline", IsFeatured = false, IsPublished = true, SortOrder = 1,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },

            // ── Messaging ───────────────────────────────────
            new()
            {
                Id = Guid.NewGuid(), Title = "Messaging and Chat",
                Summary = "Communicate with clients using in-app and SMS messaging.",
                Content = "JobFlow includes built-in messaging so you can communicate with clients without leaving the app.\n\n**In-app chat:** Open Messaging from the sidebar to view all conversations. Messages appear in real time using live updates.\n\n**SMS messaging:** JobFlow can send and receive SMS messages, keeping the conversation synced with the in-app chat thread.\n\n**Client-side:** Clients can chat with your business from the Client Hub. Their messages appear in your Messaging inbox alongside any SMS threads.\n\n**Tips:**\n- Use messaging to send appointment reminders, arrival notifications, or follow-up thanks.\n- All messages are stored and searchable, so you have a full history of client communication.",
                ArticleType = HelpArticleType.Guide, Category = HelpArticleCategory.Messaging,
                Tags = "messaging,chat,sms,communication", IsFeatured = false, IsPublished = true, SortOrder = 1,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },

            // ── Billing ─────────────────────────────────────
            new()
            {
                Id = Guid.NewGuid(), Title = "Connecting a Payment Provider",
                Summary = "Set up Stripe or Square to accept online payments.",
                Content = "JobFlow supports two payment providers: **Stripe** and **Square**. Connecting one allows your clients to pay invoices online.\n\n**Connecting Stripe:**\n1. Go to Settings → Billing & Payments.\n2. Click \"Connect Stripe.\"\n3. Follow the Stripe onboarding flow to link or create your Stripe account.\n4. Once connected, invoices automatically include a Pay Online button.\n\n**Connecting Square:**\n1. Go to Settings → Billing & Payments.\n2. Click \"Connect Square.\"\n3. Authorize JobFlow to access your Square merchant account.\n4. Payments are processed through your Square account.\n\nYou can connect one provider at a time. Switch providers at any time from the same settings page.\n\n**Payment tracking:** All payments, disputes, and refunds are tracked in the Billing & Payments section.",
                ArticleType = HelpArticleType.Guide, Category = HelpArticleCategory.Billing,
                Tags = "payments,stripe,square,connect,billing", IsFeatured = true, IsPublished = true, SortOrder = 1,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },

            // ── Branding ────────────────────────────────────
            new()
            {
                Id = Guid.NewGuid(), Title = "Customizing Your Branding",
                Summary = "Add your logo, colors, and business identity to client-facing pages.",
                Content = "Branding lets you personalize the client experience with your business identity. This feature requires the Flow plan.\n\n**What you can customize:**\n- **Logo** — Upload your business logo. It appears on invoices, estimates, and the Client Hub.\n- **Primary color** — Used for buttons, links, and accents throughout client-facing pages.\n- **Secondary color** — Used for secondary elements and backgrounds.\n- **Business name** — The display name shown to clients.\n- **Tagline** — A short slogan or description shown on client-facing pages.\n- **Footer note** — Custom text that appears at the bottom of invoices and estimates (e.g., \"Thank you for your business!\").\n\n**To update branding:**\n1. Go to Settings → Branding.\n2. Upload your logo and set your colors.\n3. Preview the changes.\n4. Save.\n\nBranding changes take effect immediately on all client-facing pages.",
                ArticleType = HelpArticleType.Guide, Category = HelpArticleCategory.Branding,
                Tags = "branding,logo,colors,flow plan,customization", IsFeatured = false, IsPublished = true, SortOrder = 1,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },

            // ── Subscription ────────────────────────────────
            new()
            {
                Id = Guid.NewGuid(), Title = "Managing Your Subscription",
                Summary = "Upgrade, downgrade, or view your current plan details.",
                Content = "Your subscription determines which features are available in JobFlow.\n\n**Viewing your plan:**\nGo to Settings → Subscription Management to see your current plan, status, and renewal date.\n\n**Upgrading:** Click \"Upgrade\" to move from the Go plan to the Flow plan. You'll get immediate access to branding, workflow settings, employee roles, and the price book.\n\n**Downgrading:** You can downgrade back to Go at any time. Flow-only features will become read-only until you re-upgrade.\n\n**Expired subscriptions:** If your subscription expires, you can still access Settings and Company profile, but job creation, invoicing, and other core features are paused until you renew.\n\n**Payment issues:** If a payment fails, you'll receive a notification. Update your payment method in Subscription Management to avoid service interruption.",
                ArticleType = HelpArticleType.Guide, Category = HelpArticleCategory.Subscription,
                Tags = "subscription,plans,upgrade,billing", IsFeatured = false, IsPublished = true, SortOrder = 1,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "What happens if my subscription expires?",
                Summary = "Learn what's available with an expired subscription.",
                Content = "If your subscription expires, you'll still be able to:\n- Access Settings and Company profile.\n- View existing data (jobs, invoices, clients).\n\nHowever, you won't be able to:\n- Create new jobs, invoices, or estimates.\n- Use the dispatch board or messaging.\n- Access Flow-plan features.\n\nTo restore full access, renew your subscription from Settings → Subscription Management. All your data is preserved — nothing is deleted when a subscription lapses.",
                ArticleType = HelpArticleType.Faq, Category = HelpArticleCategory.Subscription,
                Tags = "subscription,expired,renewal", IsFeatured = false, IsPublished = true, SortOrder = 2,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },

            // ── Settings ────────────────────────────────────
            new()
            {
                Id = Guid.NewGuid(), Title = "Configuring Settings",
                Summary = "Overview of all available settings: company, workflow, schedule, and invoicing.",
                Content = "JobFlow provides several settings areas to tailor the platform to your business.\n\n**Company Profile** (Settings → Company)\nSet your business name, address, phone, email, and default tax rate. This information appears on invoices and estimates.\n\n**Workflow Settings** (Settings → Workflow, Flow plan)\nDefine custom workflow statuses for jobs. Each status has a key, display label, and sort order. This lets you match JobFlow to your unique business process.\n\n**Schedule Settings**\nConfigure scheduling behavior:\n- **Travel buffer** — Minimum minutes between assignments.\n- **Default window** — The default time window for window-type assignments.\n- **Enforce buffer** — Prevent scheduling assignments that violate the travel buffer.\n- **Auto-notify on reschedule** — Automatically notify clients and employees when an assignment is rescheduled.\n\n**Invoicing Settings**\nSet the default invoicing workflow for new jobs: \"Send invoice after completion\" or \"Collect payment in person.\"\n\n**Price Book** (Settings → Price Book, Flow plan)\nMaintain a catalog of products and services with categories, descriptions, and prices. Pull items from the price book when creating estimates or invoices.",
                ArticleType = HelpArticleType.Guide, Category = HelpArticleCategory.Settings,
                Tags = "settings,workflow,schedule,invoicing,price book", IsFeatured = false, IsPublished = true, SortOrder = 1,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "How do I change my default tax rate?",
                Summary = "Update the tax rate applied to new invoices.",
                Content = "Your default tax rate is set at the organization level and applied automatically when creating new invoices and estimates.\n\nTo change it:\n1. Go to Settings → Company.\n2. Find the \"Default Tax Rate\" field.\n3. Enter the new percentage (e.g., 8.25 for 8.25%).\n4. Save.\n\nExisting invoices and estimates are not affected — the new rate applies only to items created after the change.",
                ArticleType = HelpArticleType.Faq, Category = HelpArticleCategory.Settings,
                Tags = "tax rate,settings,company", IsFeatured = false, IsPublished = true, SortOrder = 2,
                PublishedBy = createdBy, PublishedAt = now, CreatedBy = createdBy
            },
        };

        var changelog = new List<ChangelogEntry>
        {
            new()
            {
                Id = Guid.NewGuid(), Title = "Help Center Launch",
                Description = "Introduced the in-app Help Center with searchable guides and FAQs organized by feature category. Access it from the Help icon in the sidebar.",
                Version = "2.4.0", Category = ChangelogCategory.Feature, IsPublished = true,
                PublishedAt = now.AddDays(-2), CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Client Hub — Request Work",
                Description = "Clients can now submit new work requests directly from the Client Hub. Each request includes a subject, description, preferred date, and optional budget.",
                Version = "2.3.0", Category = ChangelogCategory.Feature, IsPublished = true,
                PublishedAt = now.AddDays(-14), CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Estimate Revision Workflow",
                Description = "Clients can request revisions on sent estimates. Business owners review, resolve, or reject revision requests — all tracked in the estimate detail view.",
                Version = "2.2.0", Category = ChangelogCategory.Feature, IsPublished = true,
                PublishedAt = now.AddDays(-30), CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Recurring Job Support",
                Description = "Jobs can now be set to recur on a weekly or monthly basis with configurable intervals, days, and end conditions.",
                Version = "2.1.0", Category = ChangelogCategory.Feature, IsPublished = true,
                PublishedAt = now.AddDays(-45), CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Square Payment Integration",
                Description = "Added Square as a payment provider option alongside Stripe. Connect your Square merchant account from Settings → Billing & Payments.",
                Version = "2.0.0", Category = ChangelogCategory.Feature, IsPublished = true,
                PublishedAt = now.AddDays(-60), CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Improved Dispatch Board Performance",
                Description = "The dispatch board now loads faster with large teams and handles drag-and-drop more smoothly on mobile browsers.",
                Version = "2.3.1", Category = ChangelogCategory.Improvement, IsPublished = true,
                PublishedAt = now.AddDays(-10), CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Better Invoice Email Formatting",
                Description = "Invoice notification emails now include a clearer layout with line-item summaries, total amount, and a prominent Pay Now button.",
                Version = "2.2.1", Category = ChangelogCategory.Improvement, IsPublished = true,
                PublishedAt = now.AddDays(-25), CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Follow-Up Automation Reliability",
                Description = "Improved the reliability of automated follow-up sequences for estimates and invoices. Retry logic now handles transient email delivery failures.",
                Version = "2.1.1", Category = ChangelogCategory.Improvement, IsPublished = true,
                PublishedAt = now.AddDays(-40), CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Fixed Invoice Status Not Updating After Payment",
                Description = "Resolved an issue where invoice status remained 'Sent' after a successful Stripe payment. Invoices now update to 'Paid' immediately.",
                Version = "2.3.2", Category = ChangelogCategory.Fix, IsPublished = true,
                PublishedAt = now.AddDays(-7), CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Fixed Duplicate Assignments on Recurring Jobs",
                Description = "Fixed a bug where recurring jobs occasionally generated duplicate assignments for the same date.",
                Version = "2.1.2", Category = ChangelogCategory.Fix, IsPublished = true,
                PublishedAt = now.AddDays(-35), CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Fixed Client Hub Magic Link Expiration",
                Description = "Magic links for the Client Hub now correctly enforce the expiration window. Previously, some links remained valid past their intended expiry.",
                Version = "2.0.1", Category = ChangelogCategory.Fix, IsPublished = true,
                PublishedAt = now.AddDays(-55), CreatedBy = createdBy
            },
        };

        foreach (var article in articles)
            await _articles.AddAsync(article);

        foreach (var entry in changelog)
            await _changelog.AddAsync(entry);

        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }
}
