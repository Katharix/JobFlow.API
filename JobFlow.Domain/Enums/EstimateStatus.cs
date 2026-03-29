namespace JobFlow.Domain.Enums;

public enum EstimateStatus
{
    Draft = 0,
    Sent = 1,
    Accepted = 2,
    Declined = 3,
    Cancelled = 4,
    Expired = 5,
    RevisionRequested = 6
}