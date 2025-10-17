using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IEmployeeInviteService
    {
        Task<Result<EmployeeInviteDto>> InviteAsync(EmployeeInvite invite);
        Task<Result<EmployeeInviteDto>> GetInviteByCode(string code);
        Task<Result<Employee>> AcceptInviteAsync(string inviteToken);
        Task<Result<string>> ResolveShortCodeAsync(string code, string? ipAddress = null);
    }
}
