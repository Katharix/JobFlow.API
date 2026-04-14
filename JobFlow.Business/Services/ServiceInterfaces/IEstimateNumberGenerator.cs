namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IEstimateNumberGenerator
{
    /// <summary>
    ///     Generates a unique, sequential estimate number scoped per organization and date.
    ///     Format: EST-YYYYMMDD-0001, EST-YYYYMMDD-0002, etc.
    /// </summary>
    Task<string> GenerateAsync(Guid organizationId);
}
