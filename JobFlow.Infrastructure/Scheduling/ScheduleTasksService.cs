using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.Extensions.Logging;

namespace JobFlow.Infrastructure.Scheduling;

[ScopedService]
public class ScheduledTasksService
{
    private readonly IJobService _jobService;
    private readonly ILogger<ScheduledTasksService> _logger;
    private readonly INotificationService _notifications;

    public ScheduledTasksService(
        INotificationService notifications,
        IJobService jobService,
        ILogger<ScheduledTasksService> logger)
    {
        _notifications = notifications;
        _jobService = jobService;
        _logger = logger;
    }

    /// <summary>
    ///     Sends reminders for jobs scheduled for tomorrow at 08:00.
    /// </summary>
    public async Task SendDailyJobRemindersAsync()
    {
        var targetDate = DateTime.UtcNow.Date.AddDays(1);

        // 1) Fetch jobs, bail out on failure
        var result = await _jobService.GetJobsByDate(targetDate);
        if (result.IsFailure)
        {
            _logger.LogWarning("Could not retrieve jobs for {Date}: {Error}", targetDate, result.Error);
            return;
        }

        // 2) Get jobs directly (no need to go through JobOrders)
        var jobs = result.Value.ToList();

        if (!jobs.Any())
        {
            _logger.LogInformation("No jobs found for {Date}", targetDate);
            return;
        }

        // 3) Fire off notifications in parallel
        var notifyTasks = jobs.Select(async job =>
        {
            try
            {
                await _notifications.SendClientJobScheduledNotificationAsync(
                    job.OrganizationClient, // ✅ direct navigation
                    job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending scheduled reminder for JobId={JobId}, ClientId={ClientId}",
                    job.Id,
                    job.OrganizationClientId);
            }
        });

        await Task.WhenAll(notifyTasks);
    }
}