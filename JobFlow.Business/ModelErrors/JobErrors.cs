namespace JobFlow.Business.ModelErrors;

public static class JobErrors
{
    public static readonly Error NotFound = Error.NotFound("Job.NotFound", "The job was not found.");
}