namespace JobFlow.Business.ModelErrors;

public static class OrganizationTypeErrors
{
    public static Error NullOrEmptyId => Error.Failure(
        "Organization Type", "The organization type Id is null or empty.");

    public static Error OrganizationTypeNotFound => Error.NotFound(
        "Organization Type", "Organization type not found.");

    public static Error NoOrganizationTypesToUpsert => Error.Conflict(
        "Organization Type", "No organization types to upsert.");
}