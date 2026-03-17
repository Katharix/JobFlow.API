using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models
{
    public class JobRecurrence : Entity
    {
        public Guid JobId { get; set; }
        public virtual Job Job { get; set; }

        public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.Weekly;

        /// <summary>
        /// Bitmask: Sunday=1, Monday=2, Tuesday=4 ... Saturday=64.
        /// (So Mon/Wed/Fri = 2 + 8 + 32 = 42)
        /// </summary>
        public int DaysOfWeekMask { get; set; }

        /// <summary>Local-time semantics; generation converts to UTC using org/user TZ later.</summary>
        public TimeSpan StartTime { get; set; } = new(9, 0, 0);

        public TimeSpan Duration { get; set; } = new(0, 30, 0);

        public ScheduleType ScheduleType { get; set; } = ScheduleType.Window;

        public DateTime StartDate { get; set; } // date-only semantics (stored as DateTime)
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>How far ahead to generate when calendar fetches.</summary>
        public int GenerateDaysAhead { get; set; } = 28;
    }
}
