using JobFlow.Business.DI;
using JobFlow.Business.Extensions;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class FollowUpAutomationService : IFollowUpAutomationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FollowUpAutomationService> _logger;
    private readonly INotificationService _notifications;
    private readonly IFollowUpJobScheduler? _scheduler;

    private readonly IRepository<FollowUpSequence> _sequences;
    private readonly IRepository<FollowUpStep> _steps;
    private readonly IRepository<FollowUpRun> _runs;
    private readonly IRepository<FollowUpExecutionLog> _logs;
    private readonly IRepository<Estimate> _estimates;
    private readonly IRepository<OrganizationClient> _clients;

    public FollowUpAutomationService(
        IUnitOfWork unitOfWork,
        INotificationService notifications,
        ILogger<FollowUpAutomationService> logger,
        IFollowUpJobScheduler? scheduler = null)
    {
        _unitOfWork = unitOfWork;
        _notifications = notifications;
        _logger = logger;
        _scheduler = scheduler;

        _sequences = unitOfWork.RepositoryOf<FollowUpSequence>();
        _steps = unitOfWork.RepositoryOf<FollowUpStep>();
        _runs = unitOfWork.RepositoryOf<FollowUpRun>();
        _logs = unitOfWork.RepositoryOf<FollowUpExecutionLog>();
        _estimates = unitOfWork.RepositoryOf<Estimate>();
        _clients = unitOfWork.RepositoryOf<OrganizationClient>();
    }

    public async Task<Result<IReadOnlyList<FollowUpSequenceDto>>> GetSequencesAsync(Guid organizationId, FollowUpSequenceType? sequenceType = null)
    {
        var query = _sequences.Query()
            .Where(x => x.OrganizationId == organizationId)
            .Include(x => x.Steps)
            .AsSplitQuery();

        if (sequenceType.HasValue)
        {
            query = query.Where(x => x.SequenceType == sequenceType.Value);
        }

        var list = await query
            .OrderBy(x => x.SequenceType)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return Result.Success<IReadOnlyList<FollowUpSequenceDto>>(list.Select(ToDto).ToList());
    }

    public async Task<Result<FollowUpSequenceDto>> UpsertSequenceAsync(Guid organizationId, FollowUpSequenceUpsertRequestDto request)
    {
        if (request.Steps is null || request.Steps.Count == 0)
        {
            return Result.Failure<FollowUpSequenceDto>(FollowUpAutomationErrors.InvalidSteps);
        }

        if (request.Steps.GroupBy(x => x.StepOrder).Any(g => g.Count() > 1))
        {
            return Result.Failure<FollowUpSequenceDto>(FollowUpAutomationErrors.DuplicateStepOrder);
        }

        FollowUpSequence? sequence;
        if (request.Id.HasValue && request.Id.Value != Guid.Empty)
        {
            sequence = await _sequences.Query()
                .Include(x => x.Steps)
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.OrganizationId == organizationId);

            if (sequence is null)
            {
                return Result.Failure<FollowUpSequenceDto>(FollowUpAutomationErrors.SequenceNotFound);
            }

            sequence.Name = request.Name.Trim();
            sequence.IsEnabled = request.IsEnabled;
            sequence.StopOnClientReply = request.StopOnClientReply;
            sequence.DefaultChannel = request.DefaultChannel;

            _steps.RemoveRange(sequence.Steps);
            sequence.Steps.Clear();
        }
        else
        {
            sequence = new FollowUpSequence
            {
                OrganizationId = organizationId,
                SequenceType = request.SequenceType,
                Name = request.Name.Trim(),
                IsEnabled = request.IsEnabled,
                StopOnClientReply = request.StopOnClientReply,
                DefaultChannel = request.DefaultChannel
            };

            await _sequences.AddAsync(sequence);
        }

        if (sequence is null)
        {
            return Result.Failure<FollowUpSequenceDto>(FollowUpAutomationErrors.SequenceNotFound);
        }

        foreach (var step in request.Steps.OrderBy(x => x.StepOrder))
        {
            sequence.Steps.Add(new FollowUpStep
            {
                StepOrder = step.StepOrder,
                DelayHours = Math.Max(0, step.DelayHours),
                ChannelOverride = step.ChannelOverride,
                MessageTemplate = step.MessageTemplate.Trim(),
                IsEscalation = step.IsEscalation
            });
        }

        await _unitOfWork.SaveChangesAsync();

        var refreshed = await _sequences.Query()
            .Include(x => x.Steps)
            .FirstAsync(x => x.Id == sequence.Id);

        return Result.Success(ToDto(refreshed));
    }

    public async Task<Result<IReadOnlyList<FollowUpRunDto>>> GetEstimateRunsAsync(Guid organizationId, Guid estimateId)
    {
        var runs = await _runs.Query()
            .Where(x => x.OrganizationId == organizationId
                        && x.SequenceType == FollowUpSequenceType.Estimate
                        && x.TriggerEntityId == estimateId)
            .Include(x => x.ExecutionLogs)
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync();

        return Result.Success<IReadOnlyList<FollowUpRunDto>>(runs.Select(ToDto).ToList());
    }

    public async Task<Result> StartEstimateSequenceAsync(Guid organizationId, Guid estimateId, Guid organizationClientId)
    {
        var estimate = await _estimates.Query()
            .FirstOrDefaultAsync(x => x.Id == estimateId && x.OrganizationId == organizationId);

        if (estimate is null)
        {
            return Result.Failure(FollowUpAutomationErrors.EstimateNotFound);
        }

        if (estimate.Status != EstimateStatus.Sent)
        {
            return Result.Success();
        }

        var sequence = await EnsureEstimateDefaultSequenceAsync(organizationId);

        if (!sequence.IsEnabled)
        {
            return Result.Success();
        }

        var hasActiveRun = await _runs.Query().AnyAsync(x =>
            x.TriggerEntityId == estimateId
            && x.SequenceType == FollowUpSequenceType.Estimate
            && (x.Status == FollowUpRunStatus.Scheduled || x.Status == FollowUpRunStatus.InProgress));

        if (hasActiveRun)
        {
            return Result.Success();
        }

        var firstStep = sequence.Steps.OrderBy(x => x.StepOrder).FirstOrDefault();
        if (firstStep is null)
        {
            return Result.Success();
        }

        var run = new FollowUpRun
        {
            FollowUpSequenceId = sequence.Id,
            OrganizationId = organizationId,
            OrganizationClientId = organizationClientId,
            TriggerEntityId = estimateId,
            SequenceType = FollowUpSequenceType.Estimate,
            Status = FollowUpRunStatus.Scheduled,
            NextStepOrder = firstStep.StepOrder,
            StartedAt = DateTimeOffset.UtcNow
        };

        await _runs.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        await ScheduleRunStepAsync(run.Id, TimeSpan.FromHours(Math.Max(0, firstStep.DelayHours)));
        return Result.Success();
    }

    public async Task<Result> StopEstimateSequenceAsync(Guid estimateId, FollowUpStopReason reason)
    {
        var activeRuns = await _runs.Query()
            .Where(x => x.SequenceType == FollowUpSequenceType.Estimate
                        && x.TriggerEntityId == estimateId
                        && (x.Status == FollowUpRunStatus.Scheduled || x.Status == FollowUpRunStatus.InProgress))
            .ToListAsync();

        if (activeRuns.Count == 0)
        {
            return Result.Success();
        }

        foreach (var run in activeRuns)
        {
            run.Status = FollowUpRunStatus.Stopped;
            run.StopReason = reason;
            run.CompletedAt = DateTimeOffset.UtcNow;
        }

        _runs.UpdateRange(activeRuns);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> StopEstimateSequencesOnClientReplyAsync(Guid organizationId, Guid organizationClientId)
    {
        var activeRuns = await _runs.Query()
            .Include(x => x.Sequence)
            .Where(x => x.OrganizationId == organizationId
                        && x.OrganizationClientId == organizationClientId
                        && x.SequenceType == FollowUpSequenceType.Estimate
                        && (x.Status == FollowUpRunStatus.Scheduled || x.Status == FollowUpRunStatus.InProgress))
            .ToListAsync();

        if (activeRuns.Count == 0)
        {
            return Result.Success();
        }

        var runsToStop = activeRuns
            .Where(x => x.Sequence?.StopOnClientReply == true)
            .ToList();

        if (runsToStop.Count == 0)
        {
            return Result.Success();
        }

        foreach (var run in runsToStop)
        {
            run.Status = FollowUpRunStatus.Stopped;
            run.StopReason = FollowUpStopReason.ClientReplied;
            run.CompletedAt = DateTimeOffset.UtcNow;
        }

        _runs.UpdateRange(runsToStop);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ExecuteRunStepAsync(Guid runId)
    {
        var run = await _runs.Query()
            .Include(x => x.Sequence!)
            .ThenInclude(x => x.Steps)
            .FirstOrDefaultAsync(x => x.Id == runId);

        if (run is null)
        {
            return Result.Failure(FollowUpAutomationErrors.RunNotFound);
        }

        if (run.Status is FollowUpRunStatus.Completed or FollowUpRunStatus.Stopped)
        {
            return Result.Success();
        }

        if (run.Sequence is null)
        {
            await StopRunAsync(run, FollowUpStopReason.NotEligible);
            return Result.Success();
        }

        var estimate = await _estimates.Query()
            .FirstOrDefaultAsync(x => x.Id == run.TriggerEntityId);

        if (estimate is null)
        {
            await StopRunAsync(run, FollowUpStopReason.NotEligible);
            return Result.Success();
        }

        if (estimate.Status == EstimateStatus.Accepted)
        {
            await StopRunAsync(run, FollowUpStopReason.EstimateAccepted);
            return Result.Success();
        }

        if (estimate.Status == EstimateStatus.Declined)
        {
            await StopRunAsync(run, FollowUpStopReason.EstimateDeclined);
            return Result.Success();
        }

        if (estimate.Status != EstimateStatus.Sent)
        {
            await StopRunAsync(run, FollowUpStopReason.NotEligible);
            return Result.Success();
        }

        var step = run.Sequence.Steps
            .OrderBy(x => x.StepOrder)
            .FirstOrDefault(x => x.StepOrder == run.NextStepOrder);

        if (step is null)
        {
            run.Status = FollowUpRunStatus.Completed;
            run.CompletedAt = DateTimeOffset.UtcNow;
            _runs.Update(run);
            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }

        var client = await _clients.Query().FirstOrDefaultAsync(x => x.Id == run.OrganizationClientId);
        if (client is null)
        {
            return Result.Failure(FollowUpAutomationErrors.ClientNotFound);
        }

        var message = BuildEstimateMessage(step.MessageTemplate, client, estimate);
        var channel = step.ChannelOverride ?? run.Sequence.DefaultChannel;

        var log = new FollowUpExecutionLog
        {
            FollowUpRunId = run.Id,
            StepOrder = step.StepOrder,
            Channel = channel,
            ScheduledFor = DateTimeOffset.UtcNow,
            AttemptedAt = DateTimeOffset.UtcNow,
            WasSent = false
        };

        try
        {
            await _notifications.SendClientEstimateFollowUpNotificationAsync(client, estimate, message);
            log.WasSent = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Follow-up execution failed for run {RunId} step {StepOrder}", run.Id, step.StepOrder);
            log.WasSent = false;
            log.FailureReason = ex.Message;
        }

        await _logs.AddAsync(log);

        run.Status = FollowUpRunStatus.InProgress;
        run.LastAttemptAt = DateTimeOffset.UtcNow;

        var nextStep = run.Sequence.Steps
            .OrderBy(x => x.StepOrder)
            .FirstOrDefault(x => x.StepOrder > step.StepOrder);

        if (nextStep is null)
        {
            run.Status = FollowUpRunStatus.Completed;
            run.CompletedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            run.NextStepOrder = nextStep.StepOrder;
        }

        _runs.Update(run);
        await _unitOfWork.SaveChangesAsync();

        if (nextStep is not null)
        {
            await ScheduleRunStepAsync(run.Id, TimeSpan.FromHours(Math.Max(0, nextStep.DelayHours)));
        }

        return Result.Success();
    }

    private async Task<FollowUpSequence> EnsureEstimateDefaultSequenceAsync(Guid organizationId)
    {
        var existing = await _sequences.Query()
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.SequenceType == FollowUpSequenceType.Estimate);

        if (existing is not null)
        {
            return existing;
        }

        var sequence = new FollowUpSequence
        {
            OrganizationId = organizationId,
            SequenceType = FollowUpSequenceType.Estimate,
            Name = "Estimate Follow-Up",
            IsEnabled = true,
            StopOnClientReply = true,
            DefaultChannel = FollowUpChannel.Email,
            Steps = new List<FollowUpStep>
            {
                new()
                {
                    StepOrder = 1,
                    DelayHours = 48,
                    MessageTemplate = "Hi {ClientFirstName}, just checking in on estimate {EstimateNumber}. Let us know if you have any questions.",
                    IsEscalation = false
                },
                new()
                {
                    StepOrder = 2,
                    DelayHours = 72,
                    MessageTemplate = "Friendly follow-up on estimate {EstimateNumber}. We have availability this week if you want to move forward.",
                    IsEscalation = false
                }
            }
        };

        await _sequences.AddAsync(sequence);
        await _unitOfWork.SaveChangesAsync();

        return sequence;
    }

    private async Task StopRunAsync(FollowUpRun run, FollowUpStopReason reason)
    {
        run.Status = FollowUpRunStatus.Stopped;
        run.StopReason = reason;
        run.CompletedAt = DateTimeOffset.UtcNow;
        _runs.Update(run);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task ScheduleRunStepAsync(Guid runId, TimeSpan delay)
    {
        if (_scheduler is not null)
        {
            await _scheduler.ScheduleRunStepAsync(runId, delay);
            return;
        }

        if (delay <= TimeSpan.Zero)
        {
            await ExecuteRunStepAsync(runId);
        }
    }

    private static string BuildEstimateMessage(string template, OrganizationClient client, Estimate estimate)
    {
        var firstName = string.IsNullOrWhiteSpace(client.FirstName) ? "there" : client.FirstName.Trim();

        return template
            .Replace("{ClientFirstName}", firstName, StringComparison.OrdinalIgnoreCase)
            .Replace("{ClientFullName}", client.ClientFullName(), StringComparison.OrdinalIgnoreCase)
            .Replace("{EstimateNumber}", estimate.EstimateNumber, StringComparison.OrdinalIgnoreCase);
    }

    private static FollowUpSequenceDto ToDto(FollowUpSequence sequence)
    {
        return new FollowUpSequenceDto(
            sequence.Id,
            sequence.OrganizationId,
            sequence.SequenceType,
            sequence.Name,
            sequence.IsEnabled,
            sequence.StopOnClientReply,
            sequence.DefaultChannel,
            sequence.Steps
                .OrderBy(x => x.StepOrder)
                .Select(x => new FollowUpStepDto(
                    x.Id,
                    x.StepOrder,
                    x.DelayHours,
                    x.ChannelOverride,
                    x.MessageTemplate,
                    x.IsEscalation))
                .ToList());
    }

    private static FollowUpRunDto ToDto(FollowUpRun run)
    {
        return new FollowUpRunDto(
            run.Id,
            run.FollowUpSequenceId,
            run.TriggerEntityId,
            run.SequenceType,
            run.Status,
            run.StopReason,
            run.NextStepOrder,
            run.StartedAt,
            run.LastAttemptAt,
            run.CompletedAt,
            run.ExecutionLogs
                .OrderBy(x => x.StepOrder)
                .ThenBy(x => x.AttemptedAt)
                .Select(x => new FollowUpExecutionLogDto(
                    x.Id,
                    x.StepOrder,
                    x.Channel,
                    x.ScheduledFor,
                    x.AttemptedAt,
                    x.WasSent,
                    x.FailureReason))
                .ToList());
    }
}
