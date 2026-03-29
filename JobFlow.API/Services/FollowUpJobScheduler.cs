using Hangfire;
using JobFlow.Business.Services.ServiceInterfaces;

namespace JobFlow.API.Services;

public class FollowUpJobScheduler : IFollowUpJobScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public FollowUpJobScheduler(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public Task ScheduleRunStepAsync(Guid runId, TimeSpan delay)
    {
        _backgroundJobClient.Schedule<IFollowUpAutomationService>(
            svc => svc.ExecuteRunStepAsync(runId),
            delay);

        return Task.CompletedTask;
    }
}
