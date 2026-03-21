using JobFlow.Domain;

namespace JobFlow.Business.ModelErrors;

public static class JobUpdateErrors
{
    public static Error JobNotFound => Error.NotFound("JobUpdate", "Job not found.");
    public static Error UpdateNotFound => Error.NotFound("JobUpdate", "Job update not found.");
    public static Error AttachmentNotFound => Error.NotFound("JobUpdate", "Update attachment not found.");
    public static Error UnauthorizedJobAccess => Error.Validation("JobUpdate.Unauthorized", "Unauthorized job access.");
}
