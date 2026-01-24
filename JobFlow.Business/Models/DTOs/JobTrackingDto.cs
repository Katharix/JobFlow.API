namespace JobFlow.Business.Models.DTOs;

public class JobTrackingUpdateDto
{
    public Guid JobId { get; set; }
    public Guid EmployeeId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
}