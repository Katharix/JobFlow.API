using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobFlow.Tests;

public class SupportChatServiceTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

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
        public TestDbContextFactory(DbContextOptions<JobFlowDbContext> options) => _options = options;
        public JobFlowDbContext CreateDbContext() => new JobFlowDbContext(_options);
    }

    private sealed class TestWebHostEnvironmentAccessor : IWebHostEnvironmentAccessor
    {
        public string WebRootPath => Path.GetTempPath();
        public string ContentRootPath => Path.GetTempPath();
    }

    private static SupportChatService CreateService(IUnitOfWork uow) =>
        new SupportChatService(uow, new TestWebHostEnvironmentAccessor());

    // ── RemoveFromQueueAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task RemoveFromQueueAsync_SetsStatusToClosed_WhenSessionIsQueued()
    {
        var uow = CreateUnitOfWork(nameof(RemoveFromQueueAsync_SetsStatusToClosed_WhenSessionIsQueued));
        var svc = CreateService(uow);
        var joinResult = await svc.JoinQueueAsync("Alice Smith", "alice@example.com");
        var sessionId = joinResult.Value.SessionId;

        var result = await svc.RemoveFromQueueAsync(sessionId);

        Assert.True(result.IsSuccess);
        var sessionResult = await svc.GetSessionAsync(sessionId);
        Assert.True(sessionResult.IsSuccess);
        Assert.Equal(SupportChatSessionStatus.Closed, sessionResult.Value.Status);
    }

    [Fact]
    public async Task RemoveFromQueueAsync_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        var uow = CreateUnitOfWork(nameof(RemoveFromQueueAsync_ReturnsNotFound_WhenSessionDoesNotExist));
        var svc = CreateService(uow);

        var result = await svc.RemoveFromQueueAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("SupportChat.SessionNotFound", result.Error.Code);
    }

    [Fact]
    public async Task RemoveFromQueueAsync_ReturnsNotFound_WhenSessionIsAlreadyClosed()
    {
        var uow = CreateUnitOfWork(nameof(RemoveFromQueueAsync_ReturnsNotFound_WhenSessionIsAlreadyClosed));
        var svc = CreateService(uow);
        var joinResult = await svc.JoinQueueAsync("Bob Jones", "bob@example.com");
        var sessionId = joinResult.Value.SessionId;
        await svc.RemoveFromQueueAsync(sessionId); // close once

        var result = await svc.RemoveFromQueueAsync(sessionId); // attempt again on closed session

        Assert.True(result.IsFailure);
        Assert.Equal("SupportChat.SessionNotFound", result.Error.Code);
    }

    [Fact]
    public async Task RemoveFromQueueAsync_SetsClosedAt_WhenSessionIsRemoved()
    {
        var uow = CreateUnitOfWork(nameof(RemoveFromQueueAsync_SetsClosedAt_WhenSessionIsRemoved));
        var svc = CreateService(uow);
        var joinResult = await svc.JoinQueueAsync("Carol White", "carol@example.com");
        var sessionId = joinResult.Value.SessionId;

        await svc.RemoveFromQueueAsync(sessionId);

        var sessionResult = await svc.GetSessionAsync(sessionId);
        Assert.NotNull(sessionResult.Value.ClosedAt);
    }

    [Fact]
    public async Task RemoveFromQueueAsync_DoesNotAffectOtherQueuedSessions()
    {
        var uow = CreateUnitOfWork(nameof(RemoveFromQueueAsync_DoesNotAffectOtherQueuedSessions));
        var svc = CreateService(uow);
        var join1 = await svc.JoinQueueAsync("Dave One", "dave@example.com");
        var join2 = await svc.JoinQueueAsync("Eve Two", "eve@example.com");

        await svc.RemoveFromQueueAsync(join1.Value.SessionId);

        var queue = await svc.GetQueueAsync();
        Assert.True(queue.IsSuccess);
        Assert.Single(queue.Value);
        Assert.Equal(join2.Value.SessionId, queue.Value[0].SessionId);
    }

    // ── JoinQueueAsync queue-full guard (exercised through GetQueueAsync) ────

    [Fact]
    public async Task JoinQueueAsync_ReturnsSuccessWithPosition_WhenQueueHasFewer5Sessions()
    {
        var uow = CreateUnitOfWork(nameof(JoinQueueAsync_ReturnsSuccessWithPosition_WhenQueueHasFewer5Sessions));
        var svc = CreateService(uow);

        var result = await svc.JoinQueueAsync("Frank New", "frank@example.com");

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.QueuePosition);
    }
}
