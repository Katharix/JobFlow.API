using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public sealed record JobUpdateAttachmentUpload(
    string FileName,
    string ContentType,
    byte[] Content,
    long SizeBytes
);

public sealed record CreateJobUpdateRequest(
    JobUpdateType Type,
    string? Message,
    JobLifecycleStatus? Status,
    IReadOnlyList<JobUpdateAttachmentUpload> Attachments
);

public sealed record JobUpdateAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSizeBytes
);

public sealed record JobUpdateAttachmentDownloadDto(
    string FileName,
    string ContentType,
    byte[] Content
);

public sealed record JobUpdateDto(
    Guid Id,
    Guid JobId,
    JobUpdateType Type,
    string? Message,
    JobLifecycleStatus? Status,
    DateTimeOffset OccurredAt,
    IReadOnlyList<JobUpdateAttachmentDto> Attachments
);
