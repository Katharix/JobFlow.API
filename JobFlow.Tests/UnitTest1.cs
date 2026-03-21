using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services;
using JobFlow.Domain.Enums;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobFlow.Tests;

public class WorkflowAndScheduleSettingsTests
{
    [Fact]
    public async Task WorkflowSettingsDefaults_WhenNoCustomStatuses()
    {
        var unitOfWork = CreateUnitOfWork(nameof(WorkflowSettingsDefaults_WhenNoCustomStatuses));
        var service = new WorkflowSettingsService(unitOfWork);

        var result = await service.GetJobLifecycleStatusesAsync(Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
        Assert.Contains(result.Value, status =>
            status.StatusKey == JobLifecycleStatus.InProgress.ToString() &&
            status.Label == "In Progress");
    }

    [Fact]
    public async Task WorkflowSettingsRejectsDuplicateKeys()
    {
        var unitOfWork = CreateUnitOfWork(nameof(WorkflowSettingsRejectsDuplicateKeys));
        var service = new WorkflowSettingsService(unitOfWork);

        var payload = new List<WorkflowStatusUpsertRequestDto>
        {
            new()
            {
                StatusKey = JobLifecycleStatus.Draft.ToString(),
                Label = "Draft",
                SortOrder = 0
            },
            new()
            {
                StatusKey = JobLifecycleStatus.Draft.ToString(),
                Label = "Draft Again",
                SortOrder = 1
            }
        };

        var result = await service.UpsertJobLifecycleStatusesAsync(Guid.NewGuid(), payload);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ScheduleSettingsDefaultValues_WhenMissing()
    {
        var unitOfWork = CreateUnitOfWork(nameof(ScheduleSettingsDefaultValues_WhenMissing));
        var service = new ScheduleSettingsService(unitOfWork);

        var result = await service.GetScheduleSettingsAsync(Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value.TravelBufferMinutes);
        Assert.Equal(120, result.Value.DefaultWindowMinutes);
        Assert.True(result.Value.EnforceTravelBuffer);
        Assert.True(result.Value.AutoNotifyReschedule);
    }

    [Fact]
    public async Task ScheduleSettingsRejectsNegativeValues()
    {
        var unitOfWork = CreateUnitOfWork(nameof(ScheduleSettingsRejectsNegativeValues));
        var service = new ScheduleSettingsService(unitOfWork);

        var result = await service.UpsertScheduleSettingsAsync(
            Guid.NewGuid(),
            new ScheduleSettingsUpsertRequestDto
            {
                TravelBufferMinutes = -5,
                DefaultWindowMinutes = 30,
                EnforceTravelBuffer = true,
                AutoNotifyReschedule = true
            });

        Assert.True(result.IsFailure);
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
}
