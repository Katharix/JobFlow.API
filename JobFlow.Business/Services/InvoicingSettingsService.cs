using JobFlow.Business.DI;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Business.Services;

[ScopedService]
public class InvoicingSettingsService : IInvoicingSettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<OrganizationInvoicingSettings> _settings;

    public InvoicingSettingsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _settings = unitOfWork.RepositoryOf<OrganizationInvoicingSettings>();
    }

    public async Task<Result<InvoicingSettingsDto>> GetInvoicingSettingsAsync(Guid organizationId)
    {
        var settings = await _settings.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId);

        if (settings == null)
        {
            return Result.Success(new InvoicingSettingsDto
            {
                DefaultWorkflow = InvoicingWorkflow.SendInvoice
            });
        }

        return Result.Success(Map(settings));
    }

    public async Task<Result<InvoicingSettingsDto>> UpsertInvoicingSettingsAsync(
        Guid organizationId,
        InvoicingSettingsUpsertRequestDto dto)
    {
        var settings = await _settings.Query()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId);

        if (settings == null)
        {
            settings = new OrganizationInvoicingSettings
            {
                OrganizationId = organizationId
            };
            _settings.Add(settings);
        }

        settings.DefaultWorkflow = dto.DefaultWorkflow;

        await _unitOfWork.SaveChangesAsync();

        return Result.Success(Map(settings));
    }

    private static InvoicingSettingsDto Map(OrganizationInvoicingSettings settings)
    {
        return new InvoicingSettingsDto
        {
            DefaultWorkflow = settings.DefaultWorkflow
        };
    }
}