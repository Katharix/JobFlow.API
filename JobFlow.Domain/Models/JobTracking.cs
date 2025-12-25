namespace JobFlow.Domain.Models;

public class JobTracking : Entity
{
    public Guid JobId { get; set; }
    public Guid EmployeeId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Job Job { get; set; } = null!;
    public User Employee { get; set; } = null!;
}