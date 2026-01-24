namespace JobFlow.Business.ModelErrors;

public static class OrganizationClientErrors
{
    public static Error NullOrEmptyId => Error.Failure(
        "Clients", "The client Id is null or empty.");

    public static Error NoClientsToShow => Error.NotFound(
        "Clients", "No clients to show.");

    public static Error FailedToCreateClient => Error.Failure(
        "Clients", "Failed to create new client.");

    public static Error NoClientFound => Error.Failure(
        "Client", "No client found");

    public static Error FailedToDeleteClient(string clientName)
    {
        return Error.Failure(
            "Clients", $"Failed to delete {clientName}");
    }

    public static Error FailedToUpdateClient(string clientName)
    {
        return Error.Failure(
            "Clients", $"Failed to update {clientName}");
    }
}