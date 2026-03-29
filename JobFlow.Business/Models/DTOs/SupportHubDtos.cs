using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public record SupportHubTicketDto(
    Guid Id,
    string Title,
    SupportHubTicketStatus Status,
    string OrganizationName,
    DateTimeOffset CreatedAt);

public record SupportHubSessionDto(
    Guid Id,
    string OrganizationName,
    string AgentName,
    SupportHubSessionStatus Status,
    DateTimeOffset? StartedAt);

public record SupportHubScreenResponseDto(
    Guid SessionId,
    string ViewerUrl);

public record SupportHubTicketCreateRequest(
    Guid OrganizationId,
    string Title,
    string? Summary,
    SupportHubTicketStatus Status);

public record SupportHubSessionCreateRequest(
    Guid OrganizationId,
    string AgentName,
    SupportHubSessionStatus Status);

public record SupportHubSeedRequest(Guid OrganizationId);

public record SupportHubSeedResponse(int TicketsCreated, int SessionsCreated);

public record SupportHubInviteDto(
    Guid Id,
    string Code,
    SupportHubInviteRole Role,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RedeemedAt,
    string? RedeemedBy);

public record SupportHubInviteCreateRequest(
    SupportHubInviteRole Role,
    DateTimeOffset? ExpiresAt);

public record SupportHubInviteRedeemRequest(string Code);

public record SupportHubInviteValidationDto(
    SupportHubInviteDto? Invite,
    string? Error);
