namespace JobFlow.Business.ModelErrors;

public static class EstimateRevisionErrors
{
    public static readonly Error EstimateNotFound =
        Error.NotFound("EstimateRevision.EstimateNotFound", "The estimate was not found.");

    public static readonly Error UnauthorizedEstimateAccess =
        Error.NotFound("EstimateRevision.UnauthorizedEstimateAccess", "The estimate was not found.");

    public static readonly Error InvalidEstimateStatus =
        Error.Conflict("EstimateRevision.InvalidEstimateStatus", "Revisions can only be requested for sent or accepted estimates.");

    public static readonly Error OpenRevisionAlreadyExists =
        Error.Conflict("EstimateRevision.OpenRevisionAlreadyExists", "A revision request is already open for this estimate.");

    public static readonly Error MessageRequired =
        Error.Validation("EstimateRevision.MessageRequired", "A revision message is required.");

    public static readonly Error MessageTooLong =
        Error.Validation("EstimateRevision.MessageTooLong", "Revision message must be 2000 characters or less.");

    public static readonly Error TooManyAttachments =
        Error.Validation("EstimateRevision.TooManyAttachments", "A maximum of 5 attachments are allowed per revision request.");

    public static readonly Error AttachmentTooLarge =
        Error.Validation("EstimateRevision.AttachmentTooLarge", "Each attachment must be 10 MB or less.");

    public static readonly Error InvalidAttachmentContentType =
        Error.Validation("EstimateRevision.InvalidAttachmentContentType", "One or more attachments use an unsupported file type.");

    public static readonly Error InvalidAttachment =
        Error.Validation("EstimateRevision.InvalidAttachment", "One or more attachments are invalid.");

    public static readonly Error RevisionRequestNotFound =
        Error.NotFound("EstimateRevision.RevisionRequestNotFound", "Revision request was not found.");

    public static readonly Error AttachmentNotFound =
        Error.NotFound("EstimateRevision.AttachmentNotFound", "Attachment was not found.");

    public static readonly Error OrganizationNotFound =
        Error.NotFound("EstimateRevision.OrganizationNotFound", "Organization was not found.");

    public static readonly Error ClientNotFound =
        Error.NotFound("EstimateRevision.ClientNotFound", "Organization client was not found.");
}
