using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class JobTemplate : Entity
{
    public Guid? OrganizationId { get; set; }
    public Guid? OrganizationTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public InvoicingWorkflow? DefaultInvoicingWorkflow { get; set; }
    public bool IsSystem { get; set; }
    public Organization? Organization { get; set; }
    public OrganizationType? OrganizationType { get; set; }
    public ICollection<JobTemplateItem> Items { get; set; } = new List<JobTemplateItem>();
}
