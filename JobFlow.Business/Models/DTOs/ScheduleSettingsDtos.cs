namespace JobFlow.Business.Models.DTOs;

public class ScheduleSettingsDto
{
    public int TravelBufferMinutes { get; set; }
    public int DefaultWindowMinutes { get; set; }
    public bool EnforceTravelBuffer { get; set; }
    public bool AutoNotifyReschedule { get; set; }
}

public class ScheduleSettingsUpsertRequestDto
{
    public int TravelBufferMinutes { get; set; }
    public int DefaultWindowMinutes { get; set; }
    public bool EnforceTravelBuffer { get; set; }
    public bool AutoNotifyReschedule { get; set; }
}
