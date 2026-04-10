using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public class JobTemplateDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? OrganizationTypeId { get; set; }
    public string? OrganizationTypeName { get; set; }
    public InvoicingWorkflow? DefaultInvoicingWorkflow { get; set; }
    public bool IsSystem { get; set; }
    public Guid? OrganizationId { get; set; }
    public List<JobTemplateItemDto> Items { get; set; } = new();
}
