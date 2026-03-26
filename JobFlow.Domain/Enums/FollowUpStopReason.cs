namespace JobFlow.Domain.Enums;

public enum FollowUpStopReason
{
    None = 0,
    ClientReplied = 1,
    EstimateAccepted = 2,
    EstimateDeclined = 3,
    InvoicePaid = 4,
    ManuallyStopped = 5,
    SequenceDisabled = 6,
    NotEligible = 7
}
