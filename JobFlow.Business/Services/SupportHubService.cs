using JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces;
using JobFlow.Business.DI;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Business.Services;

[ScopedService]
public class SupportHubService : ISupportHubService
{
    private readonly IRepository<SupportHubTicket> _tickets;
    private readonly IRepository<SupportHubSession> _sessions;
    private readonly IRepository<Organization> _organizations;
    private readonly IFrontendSettings _frontendSettings;
    private readonly IUnitOfWork _unitOfWork;

    public SupportHubService(
        IUnitOfWork unitOfWork,
        IFrontendSettings frontendSettings)
    {
        _unitOfWork = unitOfWork;
        _frontendSettings = frontendSettings;
        _tickets = unitOfWork.RepositoryOf<SupportHubTicket>();
        _sessions = unitOfWork.RepositoryOf<SupportHubSession>();
        _organizations = unitOfWork.RepositoryOf<Organization>();
    }

    public async Task<Result<List<SupportHubTicketDto>>> GetTicketsAsync()
    {
        var tickets = await _tickets.Query()
            .AsNoTracking()
            .Include(x => x.Organization)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new SupportHubTicketDto(
                x.Id,
                x.Title,
                x.Status,
                x.Organization != null ? x.Organization.OrganizationName ?? "Unknown" : "Unknown",
                new DateTimeOffset(x.CreatedAt, TimeSpan.Zero)))
            .ToListAsync();

        return Result.Success(tickets);
    }

    public async Task<Result<List<SupportHubSessionDto>>> GetSessionsAsync()
    {
        var sessions = await _sessions.Query()
            .AsNoTracking()
            .Include(x => x.Organization)
            .OrderByDescending(x => x.StartedAt ?? DateTimeOffset.MinValue)
            .Select(x => new SupportHubSessionDto(
                x.Id,
                x.Organization != null ? x.Organization.OrganizationName ?? "Unknown" : "Unknown",
                x.AgentName,
                x.Status,
                x.StartedAt))
            .ToListAsync();

        return Result.Success(sessions);
    }

    public async Task<Result<SupportHubScreenResponseDto>> CreateScreenViewAsync(Guid sessionId)
    {
        var session = await _sessions.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == sessionId);
        if (session is null)
        {
            return Result.Failure<SupportHubScreenResponseDto>(
                Error.NotFound("SupportHub.SessionNotFound", "Support session not found."));
        }

        var baseUrl = _frontendSettings.BaseUrl?.TrimEnd('/') ?? string.Empty;
        var viewerUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? $"/support-hub/sessions/{sessionId}"
            : $"{baseUrl}/support-hub/sessions/{sessionId}";

        var response = new SupportHubScreenResponseDto(sessionId, viewerUrl);
        return Result.Success(response);
    }

    public async Task<Result<SupportHubTicketDto>> CreateTicketAsync(
        SupportHubTicketCreateRequest request,
        string? createdBy)
    {
        if (request.OrganizationId == Guid.Empty)
        {
            return Result.Failure<SupportHubTicketDto>(
                Error.Validation("SupportHub.OrganizationRequired", "Organization is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result.Failure<SupportHubTicketDto>(
                Error.Validation("SupportHub.TitleRequired", "Ticket title is required."));
        }

        if (request.Title.Length > 160)
        {
            return Result.Failure<SupportHubTicketDto>(
                Error.Validation("SupportHub.TitleTooLong", "Ticket title is too long."));
        }

        if (!string.IsNullOrWhiteSpace(request.Summary) && request.Summary.Length > 500)
        {
            return Result.Failure<SupportHubTicketDto>(
                Error.Validation("SupportHub.SummaryTooLong", "Ticket summary is too long."));
        }

        var orgExists = await _organizations.ExistsAsync(x => x.Id == request.OrganizationId);
        if (!orgExists)
        {
            return Result.Failure<SupportHubTicketDto>(
                Error.NotFound("SupportHub.OrganizationNotFound", "Organization not found."));
        }

        var ticket = new SupportHubTicket
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            Title = request.Title.Trim(),
            Summary = request.Summary?.Trim(),
            Status = request.Status,
            LastActivityAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };

        await _tickets.AddAsync(ticket);
        await _unitOfWork.SaveChangesAsync();

        var orgName = await _organizations.Query()
            .Where(x => x.Id == request.OrganizationId)
            .Select(x => x.OrganizationName)
            .FirstOrDefaultAsync();

        var dto = new SupportHubTicketDto(
            ticket.Id,
            ticket.Title,
            ticket.Status,
            orgName ?? "Unknown",
            new DateTimeOffset(ticket.CreatedAt, TimeSpan.Zero));

        return Result.Success(dto);
    }

    public async Task<Result<SupportHubSessionDto>> CreateSessionAsync(
        SupportHubSessionCreateRequest request,
        string? createdBy)
    {
        if (request.OrganizationId == Guid.Empty)
        {
            return Result.Failure<SupportHubSessionDto>(
                Error.Validation("SupportHub.OrganizationRequired", "Organization is required."));
        }

        if (string.IsNullOrWhiteSpace(request.AgentName))
        {
            return Result.Failure<SupportHubSessionDto>(
                Error.Validation("SupportHub.AgentRequired", "Agent name is required."));
        }

        if (request.AgentName.Length > 120)
        {
            return Result.Failure<SupportHubSessionDto>(
                Error.Validation("SupportHub.AgentTooLong", "Agent name is too long."));
        }

        var orgExists = await _organizations.ExistsAsync(x => x.Id == request.OrganizationId);
        if (!orgExists)
        {
            return Result.Failure<SupportHubSessionDto>(
                Error.NotFound("SupportHub.OrganizationNotFound", "Organization not found."));
        }

        var session = new SupportHubSession
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            AgentName = request.AgentName.Trim(),
            Status = request.Status,
            StartedAt = request.Status == Domain.Enums.SupportHubSessionStatus.Live
                ? DateTimeOffset.UtcNow
                : null,
            CreatedBy = createdBy
        };

        await _sessions.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();

        var orgName = await _organizations.Query()
            .Where(x => x.Id == request.OrganizationId)
            .Select(x => x.OrganizationName)
            .FirstOrDefaultAsync();

        var dto = new SupportHubSessionDto(
            session.Id,
            orgName ?? "Unknown",
            session.AgentName,
            session.Status,
            session.StartedAt);

        return Result.Success(dto);
    }

    public async Task<Result<SupportHubSeedResponse>> SeedDemoAsync(
        SupportHubSeedRequest request,
        string? createdBy)
    {
        if (request.OrganizationId == Guid.Empty)
        {
            return Result.Failure<SupportHubSeedResponse>(
                Error.Validation("SupportHub.OrganizationRequired", "Organization is required."));
        }

        var orgExists = await _organizations.ExistsAsync(x => x.Id == request.OrganizationId);
        if (!orgExists)
        {
            return Result.Failure<SupportHubSeedResponse>(
                Error.NotFound("SupportHub.OrganizationNotFound", "Organization not found."));
        }

        var tickets = new List<SupportHubTicket>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                Title = "Invoice payment failed",
                Summary = "Customer card failed on invoice payment.",
                Status = Domain.Enums.SupportHubTicketStatus.Urgent,
                LastActivityAt = DateTimeOffset.UtcNow,
                CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                Title = "Crew schedule not saving",
                Summary = "Dispatch cannot persist schedule edits.",
                Status = Domain.Enums.SupportHubTicketStatus.High,
                LastActivityAt = DateTimeOffset.UtcNow.AddMinutes(-45),
                CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                Title = "Branding logo upload error",
                Summary = "Admin sees error when updating logo.",
                Status = Domain.Enums.SupportHubTicketStatus.Normal,
                LastActivityAt = DateTimeOffset.UtcNow.AddHours(-2),
                CreatedBy = createdBy
            }
        };

        var sessions = new List<SupportHubSession>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                AgentName = "Support Agent",
                Status = Domain.Enums.SupportHubSessionStatus.Live,
                StartedAt = DateTimeOffset.UtcNow.AddMinutes(-12),
                CreatedBy = createdBy
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                AgentName = "Support Agent",
                Status = Domain.Enums.SupportHubSessionStatus.Queued,
                CreatedBy = createdBy
            }
        };

        await _tickets.AddRangeAsync(tickets);
        await _sessions.AddRangeAsync(sessions);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(new SupportHubSeedResponse(tickets.Count, sessions.Count));
    }
}
