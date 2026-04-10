using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class JobTemplateService : IJobTemplateService
{
    private readonly IRepository<JobTemplate> _templates;
    private readonly IRepository<JobTemplateItem> _templateItems;
    private readonly ILogger<JobTemplateService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public JobTemplateService(ILogger<JobTemplateService> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _templates = unitOfWork.RepositoryOf<JobTemplate>();
        _templateItems = unitOfWork.RepositoryOf<JobTemplateItem>();
    }

    public async Task<Result<IEnumerable<JobTemplate>>> GetAvailableTemplatesAsync(Guid organizationId, Guid? organizationTypeId)
    {
        var data = await _templates.Query()
            .Include(t => t.Items)
            .Include(t => t.OrganizationType)
            .Where(t => (t.OrganizationId == organizationId) || (t.IsSystem && t.OrganizationTypeId == organizationTypeId))
            .OrderByDescending(t => t.IsSystem)
            .ThenBy(t => t.Name)
            .ToListAsync();

        return Result.Success<IEnumerable<JobTemplate>>(data);
    }

    public async Task<Result<JobTemplate>> GetByIdAsync(Guid organizationId, Guid templateId)
    {
        var template = await _templates.Query()
            .Include(t => t.Items)
            .Include(t => t.OrganizationType)
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
            return Result.Failure<JobTemplate>(JobTemplateErrors.JobTemplateNotFound);

        if (template.OrganizationId.HasValue && template.OrganizationId != organizationId)
            return Result.Failure<JobTemplate>(JobTemplateErrors.JobTemplateForbidden);

        return Result<JobTemplate>.Success(template);
    }

    public async Task<Result<JobTemplate>> CreateOrgTemplateAsync(Guid organizationId, JobTemplateDto dto)
    {
        var template = new JobTemplate
        {
            OrganizationId = organizationId,
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            DefaultInvoicingWorkflow = dto.DefaultInvoicingWorkflow,
            IsSystem = false
        };

        await _templates.AddAsync(template);
        await _unitOfWork.SaveChangesAsync();

        await ReplaceTemplateItemsAsync(template, dto.Items);

        return Result<JobTemplate>.Success(template);
    }

    public async Task<Result<JobTemplate>> UpdateOrgTemplateAsync(Guid organizationId, Guid templateId, JobTemplateDto dto)
    {
        var template = await _templates.Query().FirstOrDefaultAsync(t => t.Id == templateId);
        if (template == null)
            return Result.Failure<JobTemplate>(JobTemplateErrors.JobTemplateNotFound);

        if (template.IsSystem || template.OrganizationId != organizationId)
            return Result.Failure<JobTemplate>(JobTemplateErrors.JobTemplateForbidden);

        template.Name = dto.Name.Trim();
        template.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        template.DefaultInvoicingWorkflow = dto.DefaultInvoicingWorkflow;
        template.UpdatedAt = DateTime.UtcNow;

        _templates.Update(template);
        await _unitOfWork.SaveChangesAsync();

        await ReplaceTemplateItemsAsync(template, dto.Items);

        return Result<JobTemplate>.Success(template);
    }

    public async Task<Result> DeleteOrgTemplateAsync(Guid organizationId, Guid templateId)
    {
        var template = await _templates.Query().FirstOrDefaultAsync(t => t.Id == templateId);
        if (template == null)
            return Result.Failure(JobTemplateErrors.JobTemplateNotFound);

        if (template.IsSystem || template.OrganizationId != organizationId)
            return Result.Failure(JobTemplateErrors.JobTemplateForbidden);

        _templates.Remove(template);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    private async Task ReplaceTemplateItemsAsync(JobTemplate template, IEnumerable<JobTemplateItemDto> items)
    {
        var existing = await _templateItems.Query()
            .Where(item => item.TemplateId == template.Id)
            .ToListAsync();

        if (existing.Count > 0)
        {
            _templateItems.RemoveRange(existing);
            await _unitOfWork.SaveChangesAsync();
        }

        var list = items
            .Select((item, index) => new JobTemplateItem
            {
                TemplateId = template.Id,
                Name = item.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim(),
                SortOrder = item.SortOrder > 0 ? item.SortOrder : index + 1
            })
            .ToList();

        foreach (var item in list)
        {
            await _templateItems.AddAsync(item);
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
