using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IFeatureUsageService
{
    Task<FeatureUsageSummary> GetAsync(Guid organizationId);
}
