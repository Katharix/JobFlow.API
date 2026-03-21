using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public class JobDto
{
    public Guid? Id { get; set; }
    public string? Title { get; set; }
    public string? Comments { get; set; }
    public JobLifecycleStatus LifecycleStatus { get; set; }
    public InvoicingWorkflow? InvoicingWorkflow { get; set; }
    public Guid OrganizationClientId { get; set; }
    public OrganizationClientDto? OrganizationClient { get; set; }

    public IEnumerable<AssignmentDto>? Assignments { get; set; }
    public bool HasAssignments => Assignments?.Any() == true;
}

public class UpdateJobStatusRequestDto
{
    public JobLifecycleStatus Status { get; set; }
}

public sealed record ClientJobSummaryDto(
    Guid Id,
    string? Title,
    JobLifecycleStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record JobTimelineItemDto(
    string Id,
    string Type,
    string Title,
    string? Detail,
    DateTimeOffset OccurredAt,
    string? Status,
    decimal? Amount,
    Guid? InvoiceId,
    Guid? UpdateId,
    IReadOnlyList<JobTimelineAttachmentDto>? Attachments
);

public sealed record JobTimelineAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType
);