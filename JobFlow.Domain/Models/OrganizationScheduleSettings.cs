namespace JobFlow.Domain.Models;

public class OrganizationScheduleSettings : Entity
{
    public Guid OrganizationId { get; set; }

    public int TravelBufferMinutes { get; set; } = 20;
    public int DefaultWindowMinutes { get; set; } = 120;

    public bool EnforceTravelBuffer { get; set; } = true;
    public bool AutoNotifyReschedule { get; set; } = true;
}
