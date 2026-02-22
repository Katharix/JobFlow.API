namespace JobFlow.Domain.Enums;

public enum JobLifecycleStatus
{
    Draft = 0,
    Approved = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    Failed = 5
}
