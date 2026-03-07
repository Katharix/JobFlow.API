
namespace JobFlow.Business.Services
{
    using global::JobFlow.Business.DI;
    using global::JobFlow.Business.ModelErrors;
    using global::JobFlow.Business.Services.ServiceInterfaces;
    using global::JobFlow.Domain;
    using global::JobFlow.Domain.Enums;
    using global::JobFlow.Domain.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    [ScopedService]
    public class AssignmentGenerator : IAssignmentGenerator
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AssignmentGenerator> _logger;

        private readonly IRepository<Job> _jobs;
        private readonly IRepository<JobRecurrence> _recurrences;
        private readonly IRepository<Assignment> _assignments;

        public AssignmentGenerator(
            IUnitOfWork unitOfWork,
            ILogger<AssignmentGenerator> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;

            _jobs = unitOfWork.RepositoryOf<Job>();
            _recurrences = unitOfWork.RepositoryOf<JobRecurrence>();
            _assignments = unitOfWork.RepositoryOf<Assignment>();
        }

        public async Task<Result> EnsureAssignmentsExistAsync(Guid organizationId, DateTime rangeStartUtc, DateTime rangeEndUtc)
        {
            if (rangeEndUtc <= rangeStartUtc)
                return Result.Failure(AssignmentErrors.InvalidRecurrence);

            // Load active recurrences for org
            var active = await _recurrences.Query()
                .Include(r => r.Job)
                .ThenInclude(j => j.OrganizationClient)
                .Where(r =>
                    r.IsActive &&
                    r.Job.OrganizationClient.OrganizationId == organizationId)
                .ToListAsync();

            if (active.Count == 0)
                return Result.Success();

            foreach (var r in active)
            {
                await EnsureForRecurrenceAsync(r, rangeStartUtc, rangeEndUtc);
            }

            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }

        private async Task EnsureForRecurrenceAsync(JobRecurrence r, DateTime rangeStartUtc, DateTime rangeEndUtc)
        {
            // NOTE:
            // For v1 we treat recurrence StartTime as UTC-time-of-day.
            // Later you can convert from org/user timezone -> UTC.
            var startDate = r.StartDate.Date;
            var endDate = (r.EndDate?.Date ?? DateTime.UtcNow.Date.AddDays(r.GenerateDaysAhead));

            // clamp to requested range (date-only)
            var clampStart = MaxDate(startDate, rangeStartUtc.Date);
            var clampEnd = MinDate(endDate, rangeEndUtc.Date);

            if (clampEnd < clampStart)
                return;

            // fetch existing in range for this job
            var existingStarts = await _assignments.Query()
                .Where(a =>
                    a.JobId == r.JobId &&
                    a.ScheduledStart >= rangeStartUtc &&
                    a.ScheduledStart < rangeEndUtc)
                .Select(a => a.ScheduledStart)
                .ToListAsync();

            var existingSet = existingStarts
                .Select(dt => dt) // already UTC
                .ToHashSet();

            foreach (var day in EachDate(clampStart, clampEnd))
            {
                if (!MatchesDayMask(r.DaysOfWeekMask, day.DayOfWeek))
                    continue;

                if (r.Frequency == RecurrenceFrequency.BiWeekly)
                {
                    var weeks = (int)((day.Date - startDate).TotalDays / 7);
                    if (weeks % 2 != 0)
                        continue;
                }

                // monthly: run on same day-of-month as start date (simple v1)
                if (r.Frequency == RecurrenceFrequency.Monthly)
                {
                    if (day.Day != startDate.Day)
                        continue;
                }

                var startUtc = day.Date.Add(r.StartTime); // treated as UTC in v1
                var endUtc = startUtc.Add(r.Duration);

                if (startUtc < rangeStartUtc || startUtc >= rangeEndUtc)
                    continue;

                if (existingSet.Contains(startUtc))
                    continue;

                // create assignment
                var assignment = new Assignment
                {
                    JobId = r.JobId,
                    ScheduleType = r.ScheduleType,
                    ScheduledStart = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
                    ScheduledEnd = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc),
                    Status = AssignmentStatus.Scheduled
                };

                await _assignments.AddAsync(assignment);
                existingSet.Add(startUtc);

                _logger.LogInformation("Generated assignment JobId={JobId} Start={StartUtc}", r.JobId, startUtc);
            }
        }

        private static bool MatchesDayMask(int mask, DayOfWeek dow)
        {
            var bit = dow switch
            {
                DayOfWeek.Sunday => 1,
                DayOfWeek.Monday => 2,
                DayOfWeek.Tuesday => 4,
                DayOfWeek.Wednesday => 8,
                DayOfWeek.Thursday => 16,
                DayOfWeek.Friday => 32,
                DayOfWeek.Saturday => 64,
                _ => 0
            };
            return (mask & bit) == bit;
        }

        private static IEnumerable<DateTime> EachDate(DateTime startInclusive, DateTime endInclusive)
        {
            for (var d = startInclusive.Date; d <= endInclusive.Date; d = d.AddDays(1))
                yield return d;
        }

        private static DateTime MaxDate(DateTime a, DateTime b) => a > b ? a : b;
        private static DateTime MinDate(DateTime a, DateTime b) => a < b ? a : b;
    }
}
