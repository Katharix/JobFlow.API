namespace JobFlow.Business.ModelErrors;

public static class FollowUpAutomationErrors
{
    public static readonly Error SequenceNotFound =
        Error.NotFound("FollowUp.SequenceNotFound", "The follow-up sequence was not found.");

    public static readonly Error RunNotFound =
        Error.NotFound("FollowUp.RunNotFound", "The follow-up run was not found.");

    public static readonly Error EstimateNotFound =
        Error.NotFound("FollowUp.EstimateNotFound", "The estimate was not found.");

    public static readonly Error ClientNotFound =
        Error.NotFound("FollowUp.ClientNotFound", "The follow-up client was not found.");

    public static readonly Error InvalidSteps =
        Error.Validation("FollowUp.InvalidSteps", "At least one follow-up step is required.");

    public static readonly Error DuplicateStepOrder =
        Error.Validation("FollowUp.DuplicateStepOrder", "Step order values must be unique.");
}
