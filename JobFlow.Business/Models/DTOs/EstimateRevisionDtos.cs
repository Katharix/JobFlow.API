using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public sealed record EstimateRevisionAttachmentUpload(
    string FileName,
    string ContentType,
    byte[] Content,
    long SizeBytes
);

public sealed record CreateEstimateRevisionRequest(
    string Message,
    IReadOnlyList<EstimateRevisionAttachmentUpload> Attachments
);

public sealed record EstimateRevisionAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSizeBytes
);

public sealed record EstimateRevisionRequestDto(
    Guid Id,
    Guid EstimateId,
    Guid OrganizationId,
    Guid OrganizationClientId,
    int RevisionNumber,
    EstimateRevisionStatus Status,
    string RequestMessage,
    string? OrganizationResponseMessage,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset? ResolvedAt,
    IReadOnlyList<EstimateRevisionAttachmentDto> Attachments
);

public sealed record EstimateRevisionAttachmentDownloadDto(
    string FileName,
    string ContentType,
    byte[] Content
);
