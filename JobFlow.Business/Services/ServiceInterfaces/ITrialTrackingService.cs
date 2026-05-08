namespace JobFlow.Business.Services.ServiceInterfaces;

public static class TrialActivationEvents
{
    public const string JobCreated = "HAS_CREATED_JOB";
    public const string EstimateCreated = "HAS_CREATED_ESTIMATE";
    public const string InvoiceCreated = "HAS_CREATED_INVOICE";
    public const string ClientAdded = "HAS_ADDED_CLIENT";
}

public interface ITrialTrackingService
{
    Task TrackAsync(Guid organizationId, string eventKey);
}
