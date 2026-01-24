namespace JobFlow.Business.ModelErrors;

public static class OrganizationServiceErrors
{
    public static Error NullOrEmptyId => Error.Failure(
        "Organization Service", "The organization service Id is null or empty.");

    public static Error NoServiceFound => Error.NotFound(
        "Organization Service", "The service was not found.");

    public static Error NoOrganizationServicesToUpsert => Error.Conflict(
        "Organization Service", "No services to upsert.");

    public static Error NoServiceFoundForOrganizationName(string organizationName)
    {
        return Error.NotFound(
            "Organization Service", $"The service for {organizationName} was not found.");
    }
}