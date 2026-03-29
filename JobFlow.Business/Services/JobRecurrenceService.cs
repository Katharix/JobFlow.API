using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace JobFlow.Business.Services;

[ScopedService]
public class JobRecurrenceService : IJobRecurrenceService
{
    private readonly IRepository<Job> _jobs;
    private readonly IRepository<JobRecurrence> _recurrences;
    private readonly IUnitOfWork _unitOfWork;

    public JobRecurrenceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _jobs = unitOfWork.RepositoryOf<Job>();
        _recurrences = unitOfWork.RepositoryOf<JobRecurrence>();
    }

    public async Task<Result<JobRecurrence>> UpsertAsync(Guid jobId, Guid organizationId, JobRecurrenceUpsertRequest request)
    {
        if (jobId == Guid.Empty || organizationId == Guid.Empty)
            return Result.Failure<JobRecurrence>(AssignmentErrors.InvalidRecurrence);

        if (request.ScheduledEnd <= request.ScheduledStart)
            return Result.Failure<JobRecurrence>(AssignmentErrors.ScheduledEndMustBeAfterStart);

        if (request.Interval < 1)
            return Result.Failure<JobRecurrence>(AssignmentErrors.InvalidRecurrence);

        var job = await _jobs.Query()
            .Include(j => j.OrganizationClient)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job is null)
            return Result.Failure<JobRecurrence>(AssignmentErrors.JobNotFound);

        if (job.OrganizationClient?.OrganizationId != organizationId)
            return Result.Failure<JobRecurrence>(AssignmentErrors.InvalidOrganization);

        var recurrence = await _recurrences.Query()
            .FirstOrDefaultAsync(r => r.JobId == jobId);

        if (recurrence is null)
        {
            recurrence = new JobRecurrence
            {
                Id = Guid.NewGuid(),
                JobId = jobId
            };
            await _recurrences.AddAsync(recurrence);
        }

        var startDate = request.ScheduledStart.Date;
        if (request.DayOfMonth.HasValue)
        {
            var day = Math.Clamp(request.DayOfMonth.Value, 1, DateTime.DaysInMonth(startDate.Year, startDate.Month));
            startDate = new DateTime(startDate.Year, startDate.Month, day);
        }

        var endDate = ResolveEndDate(startDate, request);

        recurrence.Frequency = ResolveFrequency(request.Pattern, request.Interval);
        recurrence.DaysOfWeekMask = ResolveDaysOfWeekMask(request.DayOfWeek, request.ScheduledStart.DayOfWeek);
        recurrence.StartTime = request.ScheduledStart.TimeOfDay;
        recurrence.Duration = request.ScheduledEnd - request.ScheduledStart;
        recurrence.ScheduleType = request.ScheduleType;
        recurrence.StartDate = startDate;
        recurrence.EndDate = endDate;
        recurrence.IsActive = true;

        await _unitOfWork.SaveChangesAsync();
        return Result.Success(recurrence);
    }

    private static RecurrenceFrequency ResolveFrequency(RecurrencePattern pattern, int interval)
    {
        if (pattern == RecurrencePattern.Monthly)
            return RecurrenceFrequency.Monthly;

        return interval == 2 ? RecurrenceFrequency.BiWeekly : RecurrenceFrequency.Weekly;
    }

    private static int ResolveDaysOfWeekMask(List<int>? dayOfWeek, DayOfWeek fallback)
    {
        var days = dayOfWeek is { Count: > 0 } ? dayOfWeek : new List<int> { (int)fallback };
        var mask = 0;

        foreach (var day in days.Distinct())
        {
            mask |= day switch
            {
                0 => 1,  // Sunday
                1 => 2,  // Monday
                2 => 4,  // Tuesday
                3 => 8,  // Wednesday
                4 => 16, // Thursday
                5 => 32, // Friday
                6 => 64, // Saturday
                _ => 0
            };
        }

        return mask;
    }

    private static DateTime? ResolveEndDate(DateTime startDate, JobRecurrenceUpsertRequest request)
    {
        if (request.EndType == RecurrenceEndType.OnDate)
            return request.EndDate?.Date;

        if (request.EndType != RecurrenceEndType.AfterCount || !request.OccurrenceCount.HasValue)
            return null;

        var occurrences = Math.Max(request.OccurrenceCount.Value, 1);

        if (request.Pattern == RecurrencePattern.Monthly)
            return startDate.AddMonths(request.Interval * (occurrences - 1));

        return startDate.AddDays(7 * request.Interval * (occurrences - 1));
    }
}
