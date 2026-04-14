namespace JobFlow.Business.ModelErrors;

public static class EstimateErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Estimate.NotFound", "The estimate was not found.");

    public static readonly Error ClientNotFound =
        Error.NotFound("Estimate.ClientNotFound", "The client was not found.");

    public static readonly Error InvalidLineItems =
        Error.NotFound("Estimate.InvalidLineItems", "The estimate must contain at least one line item.");

    public static readonly Error InvalidPublicLink =
        Error.NotFound("Estimate.InvalidPublicLink", "The estimate link is invalid.");

    public static readonly Error PublicLinkExpired =
        Error.NotFound("Estimate.PublicLinkExpired", "The estimate link has expired.");

    public static readonly Error CannotRespondInCurrentStatus =
        Error.Conflict("Estimate.CannotRespondInCurrentStatus", "Only sent estimates can be accepted or declined.");

    public static readonly Error CannotEditInCurrentStatus =
        Error.Conflict("Estimate.CannotEditInCurrentStatus", "Estimates that have been sent, accepted, or declined cannot be edited.");
}