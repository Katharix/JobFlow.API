namespace JobFlow.Domain.Models;

public class OrganizationService : Entity
{
    public Guid OrganizationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public new bool IsActive { get; set; }
    public virtual Organization Organization { get; set; } = null!;
}