using JobFlow.Business;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IAiWriterService
{
    Task<Result<string>> DraftEstimateNotesAsync(Guid organizationId, string[] lineItemNames);
}
