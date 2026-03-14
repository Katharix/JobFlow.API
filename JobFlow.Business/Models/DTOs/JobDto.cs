using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public class JobDto
{
    public Guid? Id { get; set; }
    public string? Title { get; set; }
    public string? Comments { get; set; }
    public JobLifecycleStatus LifecycleStatus { get; set; }
    public Guid OrganizationClientId { get; set; }
    public OrganizationClientDto? OrganizationClient { get; set; }
    
    public IEnumerable<AssignmentDto>? Assignments { get; set; }
    public bool HasAssignments => Assignments?.Any() == true;
}