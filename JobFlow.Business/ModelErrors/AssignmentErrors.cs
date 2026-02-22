namespace JobFlow.Business.ModelErrors;

public static class AssignmentErrors
{
    // ─────────────────────────────────────────────
    // Not Found
    // ─────────────────────────────────────────────

    public static readonly Error NotFound =
        Error.NotFound("Assignment.NotFound", "Assignment not found.");

    public static readonly Error JobNotFound =
        Error.NotFound("Assignment.JobNotFound", "Job not found.");

    // ─────────────────────────────────────────────
    // Organization / Ownership
    // ─────────────────────────────────────────────

    public static readonly Error InvalidOrganization =
        Error.Validation(
            "Assignment.InvalidOrganization",
            "The assignment does not belong to the current organization."
        );

    // ─────────────────────────────────────────────
    // Scheduling
    // ─────────────────────────────────────────────

    public static readonly Error ScheduledStartRequired =
        Error.Validation(
            "Assignment.ScheduledStartRequired",
            "Scheduled start date and time are required."
        );

    public static readonly Error ScheduledEndRequiredForWindow =
        Error.Validation(
            "Assignment.ScheduledEndRequiredForWindow",
            "Scheduled end date and time are required for window scheduling."
        );

    public static readonly Error ScheduledEndMustBeAfterStart =
        Error.Validation(
            "Assignment.ScheduledEndMustBeAfterStart",
            "Scheduled end date and time must be after the scheduled start."
        );

    // ─────────────────────────────────────────────
    // Status / Lifecycle
    // ─────────────────────────────────────────────

    public static readonly Error InvalidStatusTransition =
        Error.Validation(
            "Assignment.InvalidStatusTransition",
            "The requested assignment status transition is not allowed."
        );

    public static readonly Error AlreadyCompleted =
        Error.Validation(
            "Assignment.AlreadyCompleted",
            "This assignment has already been completed."
        );

    public static readonly Error AlreadyCanceled =
        Error.Validation(
            "Assignment.AlreadyCanceled",
            "This assignment has already been canceled."
        );

    // ─────────────────────────────────────────────
    // Execution / Time Tracking
    // ─────────────────────────────────────────────

    public static readonly Error CannotStartCanceled =
        Error.Validation(
            "Assignment.CannotStartCanceled",
            "A canceled assignment cannot be started."
        );

    public static readonly Error CannotCompleteUnstarted =
        Error.Validation(
            "Assignment.CannotCompleteUnstarted",
            "An assignment must be started before it can be completed."
        );

    public static readonly Error ActualEndBeforeStart =
        Error.Validation(
            "Assignment.ActualEndBeforeStart",
            "Actual end time cannot be before actual start time."
        );
}
