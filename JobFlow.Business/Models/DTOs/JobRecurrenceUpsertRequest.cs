using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public enum RecurrencePattern
{
    Weekly = 1,
    Monthly = 2
}

public enum RecurrenceEndType
{
    Never = 0,
    OnDate = 1,
    AfterCount = 2
}

public class JobRecurrenceUpsertRequest
{
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }

    public ScheduleType ScheduleType { get; set; } = ScheduleType.Window;

    public RecurrencePattern Pattern { get; set; } = RecurrencePattern.Weekly;
    public int Interval { get; set; } = 1;

    public List<int>? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }

    public RecurrenceEndType EndType { get; set; } = RecurrenceEndType.Never;
    public DateTime? EndDate { get; set; }
    public int? OccurrenceCount { get; set; }
}
