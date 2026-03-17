using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IPdfGenerator
{
    /// <summary>
    ///     Generate a PDF byte array for the given Invoice, including line-items, totals, etc.
    /// </summary>
    Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice);

    /// <summary>
    ///     Generate a PDF byte array for the given Estimate.
    /// </summary>
    Task<byte[]> GenerateEstimatePdfAsync(Estimate estimate);
}