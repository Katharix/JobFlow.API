using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.Scheduling
{
    [ScopedService]
    public class ScheduledTasksService
    {
        private readonly INotificationService _notifications;
        private readonly IJobService _jobService;
        private readonly ILogger<ScheduledTasksService> _logger;

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
        /// Sends reminders for jobs scheduled for tomorrow at 08:00.
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

            // 2) Flatten to all OrganizationClient–Job pairs
            var clientJobs = result.Value
                .SelectMany(org => org.OrganizationClientJobs)  // each has OrganizationClient + Job
                .ToList();

            if (!clientJobs.Any())
            {
                _logger.LogInformation("No client jobs found for {Date}", targetDate);
                return;
            }

            // 3) Fire off notifications in parallel
            var notifyTasks = clientJobs.Select(async ocj =>
            {
                try
                {
                    await _notifications.SendClientJobScheduledNotificationAsync(
                        ocj.OrganizationClient,
                        ocj.Job);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sending scheduled reminder for JobId={JobId}, ClientId={ClientId}",
                        ocj.Job.Id,
                        ocj.OrganizationClient.Id);
                }
            });

            await Task.WhenAll(notifyTasks);
        }

    }
}
