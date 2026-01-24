namespace JobFlow.API.Models;

public class JobDto
{
    public Guid? Id { get; set; }
    public string? Title { get; set; }
    public string? Comments { get; set; }

    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public Guid OrganizationClientId { get; set; }
}