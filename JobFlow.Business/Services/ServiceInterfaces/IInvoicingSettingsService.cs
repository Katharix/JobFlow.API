using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IInvoicingSettingsService
{
    Task<Result<InvoicingSettingsDto>> GetInvoicingSettingsAsync(Guid organizationId);
    Task<Result<InvoicingSettingsDto>> UpsertInvoicingSettingsAsync(
        Guid organizationId,
        InvoicingSettingsUpsertRequestDto dto);
}