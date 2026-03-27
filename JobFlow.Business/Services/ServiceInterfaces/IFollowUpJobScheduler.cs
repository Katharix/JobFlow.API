namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IFollowUpJobScheduler
{
    Task ScheduleRunStepAsync(Guid runId, TimeSpan delay);
}
