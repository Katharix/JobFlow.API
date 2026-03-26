using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public sealed record FollowUpStepUpsertRequestDto(
    int StepOrder,
    int DelayHours,
    FollowUpChannel? ChannelOverride,
    string MessageTemplate,
    bool IsEscalation
);

public sealed record FollowUpSequenceUpsertRequestDto(
    Guid? Id,
    FollowUpSequenceType SequenceType,
    string Name,
    bool IsEnabled,
    bool StopOnClientReply,
    FollowUpChannel DefaultChannel,
    IReadOnlyList<FollowUpStepUpsertRequestDto> Steps
);

public sealed record FollowUpStepDto(
    Guid Id,
    int StepOrder,
    int DelayHours,
    FollowUpChannel? ChannelOverride,
    string MessageTemplate,
    bool IsEscalation
);

public sealed record FollowUpSequenceDto(
    Guid Id,
    Guid OrganizationId,
    FollowUpSequenceType SequenceType,
    string Name,
    bool IsEnabled,
    bool StopOnClientReply,
    FollowUpChannel DefaultChannel,
    IReadOnlyList<FollowUpStepDto> Steps
);

public sealed record FollowUpExecutionLogDto(
    Guid Id,
    int StepOrder,
    FollowUpChannel Channel,
    DateTimeOffset ScheduledFor,
    DateTimeOffset AttemptedAt,
    bool WasSent,
    string? FailureReason
);

public sealed record FollowUpRunDto(
    Guid Id,
    Guid FollowUpSequenceId,
    Guid TriggerEntityId,
    FollowUpSequenceType SequenceType,
    FollowUpRunStatus Status,
    FollowUpStopReason StopReason,
    int NextStepOrder,
    DateTimeOffset StartedAt,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<FollowUpExecutionLogDto> Logs
);
