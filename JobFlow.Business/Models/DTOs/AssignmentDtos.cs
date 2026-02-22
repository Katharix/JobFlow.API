using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public class AssignmentDto
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }

    public ScheduleType ScheduleType { get; set; }

    public DateTimeOffset ScheduledStart { get; set; }
    public DateTimeOffset? ScheduledEnd { get; set; }

    public DateTimeOffset? ActualStart { get; set; }
    public DateTimeOffset? ActualEnd { get; set; }

    public AssignmentStatus Status { get; set; }

    public string? Address1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    public string? Notes { get; set; }

    // Useful for UI calendar
    public string? JobTitle { get; set; }
    public Guid OrganizationClientId { get; set; }
    public string? ClientName { get; set; }
}

public class CreateAssignmentRequestDto
{
    public ScheduleType ScheduleType { get; set; } = ScheduleType.Window;

    public DateTimeOffset ScheduledStart { get; set; }
    public DateTimeOffset? ScheduledEnd { get; set; }

    // Optional override address
    public string? Address1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    public string? Notes { get; set; }
}

public class UpdateAssignmentScheduleRequestDto
{
    public ScheduleType ScheduleType { get; set; } = ScheduleType.Window;

    public DateTimeOffset ScheduledStart { get; set; }
    public DateTimeOffset? ScheduledEnd { get; set; }
}

public class UpdateAssignmentStatusRequestDto
{
    public AssignmentStatus Status { get; set; }
    public DateTimeOffset? ActualStart { get; set; }
    public DateTimeOffset? ActualEnd { get; set; }
}