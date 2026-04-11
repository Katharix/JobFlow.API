using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobFlow.Tests;

public class FollowUpAutomationServiceTests
{
    [Fact]
    public async Task StartEstimateSequenceAsync_CreatesDefaultSequenceAndSingleActiveRun()
    {
        var orgId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var estimateId = Guid.NewGuid();
        var unitOfWork = CreateUnitOfWork(nameof(StartEstimateSequenceAsync_CreatesDefaultSequenceAndSingleActiveRun));

        await SeedOrganizationClientEstimateAsync(unitOfWork, orgId, clientId, estimateId, EstimateStatus.Sent);

        var service = CreateService(unitOfWork);

        var first = await service.StartEstimateSequenceAsync(orgId, estimateId, clientId);
        var second = await service.StartEstimateSequenceAsync(orgId, estimateId, clientId);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);

        var sequenceCount = await unitOfWork.RepositoryOf<FollowUpSequence>()
            .Query()
            .CountAsync(x => x.OrganizationId == orgId && x.SequenceType == FollowUpSequenceType.Estimate);

        var runCount = await unitOfWork.RepositoryOf<FollowUpRun>()
            .Query()
            .CountAsync(x => x.OrganizationId == orgId
                             && x.TriggerEntityId == estimateId
                             && (x.Status == FollowUpRunStatus.Scheduled || x.Status == FollowUpRunStatus.InProgress));

        Assert.Equal(1, sequenceCount);
        Assert.Equal(1, runCount);
    }

    [Fact]
    public async Task StopEstimateSequencesOnClientReplyAsync_StopsOnlySequencesConfiguredToStop()
    {
        var orgId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var estimateIdA = Guid.NewGuid();
        var estimateIdB = Guid.NewGuid();
        var estimateIdC = Guid.NewGuid();
        var estimateIdD = Guid.NewGuid();

        var unitOfWork = CreateUnitOfWork(nameof(StopEstimateSequencesOnClientReplyAsync_StopsOnlySequencesConfiguredToStop));

        await EnsureOrganizationAsync(unitOfWork, orgId, "Reply Org");
        await EnsureClientAsync(unitOfWork, orgId, clientId);

        var stopSequence = new FollowUpSequence
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            SequenceType = FollowUpSequenceType.Estimate,
            Name = "Stop On Reply",
            IsEnabled = true,
            StopOnClientReply = true
        };

        var keepSequence = new FollowUpSequence
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            SequenceType = FollowUpSequenceType.Estimate,
            Name = "Do Not Stop",
            IsEnabled = true,
            StopOnClientReply = false
        };

        await unitOfWork.RepositoryOf<FollowUpSequence>().AddRangeAsync(new[] { stopSequence, keepSequence });
        await unitOfWork.SaveChangesAsync();

        var runShouldStopScheduled = new FollowUpRun
        {
            Id = Guid.NewGuid(),
            FollowUpSequenceId = stopSequence.Id,
            OrganizationId = orgId,
            OrganizationClientId = clientId,
            TriggerEntityId = estimateIdA,
            SequenceType = FollowUpSequenceType.Estimate,
            Status = FollowUpRunStatus.Scheduled,
            NextStepOrder = 1
        };

        var runShouldStopInProgress = new FollowUpRun
        {
            Id = Guid.NewGuid(),
            FollowUpSequenceId = stopSequence.Id,
            OrganizationId = orgId,
            OrganizationClientId = clientId,
            TriggerEntityId = estimateIdB,
            SequenceType = FollowUpSequenceType.Estimate,
            Status = FollowUpRunStatus.InProgress,
            NextStepOrder = 2
        };

        var runAlreadyCompleted = new FollowUpRun
        {
            Id = Guid.NewGuid(),
            FollowUpSequenceId = stopSequence.Id,
            OrganizationId = orgId,
            OrganizationClientId = clientId,
            TriggerEntityId = estimateIdC,
            SequenceType = FollowUpSequenceType.Estimate,
            Status = FollowUpRunStatus.Completed,
            StopReason = FollowUpStopReason.None,
            CompletedAt = DateTimeOffset.UtcNow,
            NextStepOrder = 3
        };

        var runShouldRemainActive = new FollowUpRun
        {
            Id = Guid.NewGuid(),
            FollowUpSequenceId = keepSequence.Id,
            OrganizationId = orgId,
            OrganizationClientId = clientId,
            TriggerEntityId = estimateIdD,
            SequenceType = FollowUpSequenceType.Estimate,
            Status = FollowUpRunStatus.Scheduled,
            NextStepOrder = 1
        };

        await unitOfWork.RepositoryOf<FollowUpRun>().AddRangeAsync(new[]
        {
            runShouldStopScheduled,
            runShouldStopInProgress,
            runAlreadyCompleted,
            runShouldRemainActive
        });
        await unitOfWork.SaveChangesAsync();

        var service = CreateService(unitOfWork);
        var result = await service.StopEstimateSequencesOnClientReplyAsync(orgId, clientId);

        Assert.True(result.IsSuccess);

        var runs = await unitOfWork.RepositoryOf<FollowUpRun>()
            .QueryWithNoTracking()
            .Where(x => x.OrganizationId == orgId && x.OrganizationClientId == clientId)
            .ToListAsync();

        var stoppedScheduled = runs.Single(x => x.Id == runShouldStopScheduled.Id);
        var stoppedInProgress = runs.Single(x => x.Id == runShouldStopInProgress.Id);
        var unchangedCompleted = runs.Single(x => x.Id == runAlreadyCompleted.Id);
        var unchangedActive = runs.Single(x => x.Id == runShouldRemainActive.Id);

        Assert.Equal(FollowUpRunStatus.Stopped, stoppedScheduled.Status);
        Assert.Equal(FollowUpStopReason.ClientReplied, stoppedScheduled.StopReason);
        Assert.NotNull(stoppedScheduled.CompletedAt);

        Assert.Equal(FollowUpRunStatus.Stopped, stoppedInProgress.Status);
        Assert.Equal(FollowUpStopReason.ClientReplied, stoppedInProgress.StopReason);
        Assert.NotNull(stoppedInProgress.CompletedAt);

        Assert.Equal(FollowUpRunStatus.Completed, unchangedCompleted.Status);
        Assert.Equal(FollowUpRunStatus.Scheduled, unchangedActive.Status);
        Assert.Equal(FollowUpStopReason.None, unchangedActive.StopReason);
    }

    private static FollowUpAutomationService CreateService(JobFlowUnitOfWork unitOfWork)
    {
        return new FollowUpAutomationService(
            unitOfWork,
            new NoOpNotificationService(),
            NullLogger<FollowUpAutomationService>.Instance,
            scheduler: null);
    }

    private static async Task SeedOrganizationClientEstimateAsync(
        JobFlowUnitOfWork unitOfWork,
        Guid orgId,
        Guid clientId,
        Guid estimateId,
        EstimateStatus estimateStatus)
    {
        await EnsureOrganizationAsync(unitOfWork, orgId, "Estimate Org");
        await EnsureClientAsync(unitOfWork, orgId, clientId);

        var estimate = new Estimate
        {
            Id = estimateId,
            OrganizationId = orgId,
            OrganizationClientId = clientId,
            EstimateNumber = "EST-0001",
            PublicToken = Guid.NewGuid().ToString("N"),
            Status = estimateStatus,
            SentAt = estimateStatus == EstimateStatus.Sent ? DateTimeOffset.UtcNow : null
        };

        await unitOfWork.RepositoryOf<Estimate>().AddAsync(estimate);
        await unitOfWork.SaveChangesAsync();
    }

    private static async Task EnsureOrganizationAsync(JobFlowUnitOfWork unitOfWork, Guid organizationId, string name)
    {
        var organization = new Organization
        {
            Id = organizationId,
            OrganizationTypeId = Guid.NewGuid(),
            OrganizationName = name,
            IsActive = true
        };

        await unitOfWork.RepositoryOf<Organization>().AddAsync(organization);
        await unitOfWork.SaveChangesAsync();
    }

    private static async Task EnsureClientAsync(JobFlowUnitOfWork unitOfWork, Guid organizationId, Guid clientId)
    {
        var client = new OrganizationClient
        {
            Id = clientId,
            OrganizationId = organizationId,
            FirstName = "Pat",
            LastName = "Client",
            PhoneNumber = "+15555555555",
            IsActive = true
        };

        await unitOfWork.RepositoryOf<OrganizationClient>().AddAsync(client);
        await unitOfWork.SaveChangesAsync();
    }

    private static JobFlowUnitOfWork CreateUnitOfWork(string databaseName)
    {
        var options = new DbContextOptionsBuilder<JobFlowDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var factory = new TestDbContextFactory(options);
        return new JobFlowUnitOfWork(NullLogger<JobFlowUnitOfWork>.Instance, factory);
    }

    private sealed class TestDbContextFactory : IDbContextFactory<JobFlowDbContext>
    {
        private readonly DbContextOptions<JobFlowDbContext> _options;

        public TestDbContextFactory(DbContextOptions<JobFlowDbContext> options)
        {
            _options = options;
        }

        public JobFlowDbContext CreateDbContext()
        {
            return new JobFlowDbContext(_options);
        }
    }

    private sealed class NoOpNotificationService : INotificationService
    {
        public Task SendOrganizationWelcomeNotificationAsync(Organization organization) => Task.CompletedTask;
        public Task SendOrganizationSubsciptionPaymentFailedNotificationAsync(Organization organization) => Task.CompletedTask;
        public Task SendOrganizationPaymentReceivedNotificationAsync(Organization organization) => Task.CompletedTask;
        public Task SendClientWelcomeNotificationAsync(OrganizationClient client) => Task.CompletedTask;
        public Task SendClientJobCreatedNotificationAsync(OrganizationClient client, Job job) => Task.CompletedTask;
        public Task SendClientJobScheduledNotificationAsync(OrganizationClient client, Job job) => Task.CompletedTask;
        public Task SendClientJobRescheduledNotificationAsync(OrganizationClient client, Job job, DateTimeOffset previousStart, DateTimeOffset? previousEnd, DateTimeOffset newStart, DateTimeOffset? newEnd) => Task.CompletedTask;
        public Task SendClientInvoiceCreatedNotificationAsync(OrganizationClient client, Invoice invoice, string? linkOverride = null) => Task.CompletedTask;
        public Task SendClientInvoiceReminderNotificationAsync(OrganizationClient client, Invoice invoice, string? linkOverride = null) => Task.CompletedTask;
        public Task SendClientPaymentReceivedNotificationAsync(OrganizationClient client, Invoice invoice) => Task.CompletedTask;
        public Task SendClientJobTrackingEtaNotificationAsync(OrganizationClient client, Job job, int etaMinutes) => Task.CompletedTask;
        public Task SendClientJobTrackingArrivalNotificationAsync(OrganizationClient client, Job job) => Task.CompletedTask;
        public Task SendClientEstimateSentNotificationAsync(OrganizationClient client, Estimate estimate) => Task.CompletedTask;
        public Task SendClientEstimateFollowUpNotificationAsync(OrganizationClient client, Estimate estimate, string message) => Task.CompletedTask;
        public Task SendOrganizationEstimateRevisionRequestedNotificationAsync(Organization organization, OrganizationClient client, Estimate estimate, string revisionMessage) => Task.CompletedTask;
        public Task SendOrganizationEstimateAcceptedNotificationAsync(Organization organization, OrganizationClient client, Estimate estimate) => Task.CompletedTask;
        public Task SendOrganizationEstimateDeclinedNotificationAsync(Organization organization, OrganizationClient client, Estimate estimate) => Task.CompletedTask;
        public Task SendOrganizationInvoicePaymentReceivedNotificationAsync(Organization organization, OrganizationClient client, Invoice invoice, decimal amountPaid) => Task.CompletedTask;
        public Task SendOrganizationClientChatMessageNotificationAsync(Organization organization, OrganizationClient client, string messagePreview) => Task.CompletedTask;
        public Task SendOrganizationClientJobUpdateNotificationAsync(Organization organization, OrganizationClient client, Job job, string updateMessage) => Task.CompletedTask;
        public Task SendOrganizationClientPortalMagicLinkAsync(OrganizationClient client, string magicLink) => Task.CompletedTask;
        public Task SendEmployeeInviteNotificationAsync(EmployeeInvite invite) => Task.CompletedTask;
    }
}
