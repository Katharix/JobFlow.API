
namespace JobFlow.Business.ModelErrors
{
    public static class OrganizationErrors
    {
        public static Error OrganizationNotFound => Error.NotFound(
            "Organization", "Organization not found.");
        public static Error NullOrEmptyId => Error.Failure(
            "Organization", "The organization Id is null or empty.");
    }
}
