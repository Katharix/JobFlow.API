using JobFlow.Domain;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IOrganizationClientPortalService
{
    Task<Result> SendMagicLinkAsync(Guid organizationId, Guid organizationClientId, string emailAddress);

    /// <summary>
    /// Validates the token and returns the OrganizationClient if valid.
    /// </summary>
    Task<Result<OrganizationClient>> RedeemMagicLinkAsync(string token);
}
