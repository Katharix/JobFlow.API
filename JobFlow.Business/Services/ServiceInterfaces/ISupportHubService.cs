using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface ISupportHubService
{
    Task<Result<List<SupportHubTicketDto>>> GetTicketsAsync();
    Task<Result<List<SupportHubSessionDto>>> GetSessionsAsync();
    Task<Result<SupportHubScreenResponseDto>> CreateScreenViewAsync(Guid sessionId);
    Task<Result<SupportHubTicketDto>> CreateTicketAsync(SupportHubTicketCreateRequest request, string? createdBy);
    Task<Result<SupportHubSessionDto>> CreateSessionAsync(SupportHubSessionCreateRequest request, string? createdBy);
    Task<Result<SupportHubSeedResponse>> SeedDemoAsync(SupportHubSeedRequest request, string? createdBy);
}
