using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IEmployeeInviteService
{
    Task<Result<EmployeeInviteDto>> InviteAsync(EmployeeInvite invite);
    Task<Result<List<EmployeeInviteDto>>> GetByOrganizationAsync(Guid organizationId);
    Task<Result> RevokeAsync(Guid inviteId, Guid organizationId);
    Task<Result<EmployeeInviteDto>> GetInviteByCode(string code);
    Task<Result<EmployeeDto>> AcceptInviteAsync(Guid inviteToken);
    Task<Result<string>> ResolveShortCodeAsync(string code, string? ipAddress = null);
}