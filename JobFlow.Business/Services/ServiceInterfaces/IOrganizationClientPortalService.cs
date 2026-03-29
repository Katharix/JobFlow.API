using JobFlow.Domain;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IOrganizationClientPortalService
{
    Task<Result> SendMagicLinkAsync(Guid organizationId, Guid organizationClientId, string emailAddress, string? returnUrl = null);

    Task<Result<string>> SendMagicLinkWithUrlAsync(
        Guid organizationId,
        Guid organizationClientId,
        string emailAddress,
        string? returnUrl = null);

    Task<Result<string>> CreateMagicLinkAsync(
        Guid organizationId,
        Guid organizationClientId,
        string emailAddress,
        string? returnUrl = null);

    /// <summary>
    /// Validates the token and returns the OrganizationClient if valid.
    /// </summary>
    Task<Result<OrganizationClient>> RedeemMagicLinkAsync(string token);
}
