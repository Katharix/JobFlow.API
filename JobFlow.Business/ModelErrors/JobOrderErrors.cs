namespace JobFlow.Business.ModelErrors;

public static class JobOrderErrors
{
    public static readonly Error JobNotFound = Error.Validation("Job.NotFound", "The specified job does not exist.");

    public static readonly Error OrderNotFound =
        Error.Validation("Order.NotFound", "The specified order does not exist.");

    public static readonly Error JobAlreadyLinkedToOrder =
        Error.Validation("JobOrder.Exists", "The job is already linked to the order.");

    public static readonly Error JobOrderLinkNotFound =
        Error.Validation("JobOrder.NotFound", "No link found between the job and the order.");
}