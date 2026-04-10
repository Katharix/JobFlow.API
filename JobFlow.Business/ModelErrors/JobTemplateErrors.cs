namespace JobFlow.Business.ModelErrors;

public static class JobTemplateErrors
{
    public static Error JobTemplateNotFound => Error.NotFound(
        "JobTemplate.NotFound",
        "Job template not found.");

    public static Error JobTemplateForbidden => Error.Failure(
        "JobTemplate.Forbidden",
        "You do not have access to this template.");
}
