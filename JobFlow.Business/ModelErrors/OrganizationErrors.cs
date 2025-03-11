
namespace JobFlow.Business.ModelErrors
{
    public static class OrganizationErrors
    {
        public static Error OrganizationNotFound => Error.NotFound(
            "Organization", "Organization not found.");
    }
}
