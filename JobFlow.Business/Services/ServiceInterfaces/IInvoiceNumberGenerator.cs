namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IInvoiceNumberGenerator
{
    /// <summary>
    ///     Generates a unique, sequential invoice number scoped per organization and year.
    ///     Sequence resets to 0001 each year. Format: YYYY-0001, YYYY-0002, etc.
    /// </summary>
    Task<string> GenerateAsync(Guid organizationId);
}