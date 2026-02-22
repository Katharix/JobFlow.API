namespace JobFlow.API.Models;

public class ScheduleJobRequest
{
    public Guid Id { get; set; }
    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
}