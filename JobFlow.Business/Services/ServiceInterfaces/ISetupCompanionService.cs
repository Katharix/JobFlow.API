using JobFlow.Business;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface ISetupCompanionService
{
    Task<Result> TrackEventAsync(Guid organizationId, string sessionId, string questionKey, string? answerKey);
}
