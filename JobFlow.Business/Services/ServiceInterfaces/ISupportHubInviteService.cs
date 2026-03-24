using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface ISupportHubInviteService
{
    Task<Result<SupportHubInviteDto>> CreateInviteAsync(SupportHubInviteCreateRequest request, string? createdBy);
    Task<Result<List<SupportHubInviteDto>>> GetActiveInvitesAsync();
    Task<Result<SupportHubInviteValidationDto>> ValidateInviteAsync(string code);
    Task<Result<SupportHubInviteDto>> RedeemInviteAsync(string code, string? redeemedBy);
}
