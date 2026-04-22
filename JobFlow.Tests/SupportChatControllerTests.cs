using JobFlow.API.Controllers;
using JobFlow.API.Hubs;
using JobFlow.Business;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace JobFlow.Tests;

public class SupportChatControllerTests
{
    // ── Stubs ─────────────────────────────────────────────────────────────────

    private sealed class StubChatService : ISupportChatService
    {
        private readonly int _queueSize;
        private readonly bool _removeSucceeds;

        public StubChatService(int queueSize = 0, bool removeSucceeds = true)
        {
            _queueSize = queueSize;
            _removeSucceeds = removeSucceeds;
        }

        public Task<Result<List<SupportChatQueueItemDto>>> GetQueueAsync()
        {
            var items = Enumerable.Range(1, _queueSize)
                .Select(i => new SupportChatQueueItemDto(
                    Guid.NewGuid(), $"Customer {i}", $"cust{i}@test.com", i, 60 * i, DateTime.UtcNow))
                .ToList();
            return Task.FromResult(Result.Success(items));
        }

        public Task<Result<SupportChatJoinQueueResponse>> JoinQueueAsync(
            string customerName, string customerEmail, Guid? customerId = null) =>
            Task.FromResult(Result.Success(new SupportChatJoinQueueResponse(Guid.NewGuid(), 1, 60)));

        public Task<Result> RemoveFromQueueAsync(Guid sessionId) =>
            Task.FromResult(_removeSucceeds
                ? Result.Success()
                : Result.Failure(Error.NotFound("SupportChat.SessionNotFound", "Session not found or not in queue.")));

        public Task<Result<SupportChatSessionDto>> PickNextCustomerAsync(Guid repId, string repName) => throw new NotImplementedException();
        public Task<Result<SupportChatSessionDto>> PickCustomerAsync(Guid sessionId, Guid repId, string repName) => throw new NotImplementedException();
        public Task<Result<SupportChatMessageDto>> SendMessageAsync(SupportChatSendMessageRequest request) => throw new NotImplementedException();
        public Task<Result<List<SupportChatMessageDto>>> GetSessionMessagesAsync(Guid sessionId) => throw new NotImplementedException();
        public Task<Result> CloseSessionAsync(Guid sessionId) => throw new NotImplementedException();
        public Task<Result<SupportChatFileUploadResponse>> UploadFileAsync(Stream stream, string fileName, string contentType) => throw new NotImplementedException();
        public Task<Result<SupportChatValidateCustomerResponse>> ValidateCustomerAsync(string email) => throw new NotImplementedException();
        public Task<Result<SupportChatSessionDto>> GetSessionAsync(Guid sessionId) => throw new NotImplementedException();
    }

    private sealed class NullUserService : IUserService
    {
        public Task<Result<IEnumerable<User>>> GetAllUsers() => throw new NotImplementedException();
        public Task<Result<User>> GetUserById(Guid userId) => throw new NotImplementedException();
        public Task<Result<User>> GetUserByFirebaseUid(string uid) => throw new NotImplementedException();
        public Task<Result<User>> UpsertUser(User model) => throw new NotImplementedException();
        public Task<Result> DeleteUser(Guid userId) => throw new NotImplementedException();
        public Task<Result<User>> GetUserByEmail(string email) => throw new NotImplementedException();
        public Task<Result> AssignRole(Guid userId, string role) => throw new NotImplementedException();
        public Task<Result<UserProfileDto>> GetProfileByFirebaseUid(string uid) => throw new NotImplementedException();
        public Task<Result<UserProfileDto>> UpdateProfile(string uid, UserProfileUpdateRequest request) => throw new NotImplementedException();
    }

    private sealed class NullHubContext : IHubContext<SupportChatHub>
    {
        public IHubClients Clients { get; } = new NullHubClients();
        public IGroupManager Groups => throw new NotImplementedException();

        private sealed class NullHubClients : IHubClients
        {
            private static readonly NullClientProxy Proxy = new();
            public IClientProxy All => Proxy;
            public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => Proxy;
            public IClientProxy Client(string connectionId) => Proxy;
            public IClientProxy Clients(IReadOnlyList<string> connectionIds) => Proxy;
            public IClientProxy Group(string groupName) => Proxy;
            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Proxy;
            public IClientProxy Groups(IReadOnlyList<string> groupNames) => Proxy;
            public IClientProxy User(string userId) => Proxy;
            public IClientProxy Users(IReadOnlyList<string> userIds) => Proxy;
        }

        private sealed class NullClientProxy : IClientProxy
        {
            public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default) =>
                Task.CompletedTask;
        }
    }

    private static SupportChatController CreateController(ISupportChatService chatService)
    {
        var controller = new SupportChatController(chatService, new NullUserService(), new NullHubContext());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    // ── RemoveFromQueue ───────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveFromQueue_Returns200_WhenSessionRemovedSuccessfully()
    {
        var controller = CreateController(new StubChatService(removeSucceeds: true));

        var result = await controller.RemoveFromQueue(Guid.NewGuid());

        Assert.Equal(200, ((IStatusCodeHttpResult)result).StatusCode);
    }

    [Fact]
    public async Task RemoveFromQueue_Returns404_WhenSessionNotFound()
    {
        var controller = CreateController(new StubChatService(removeSucceeds: false));

        var result = await controller.RemoveFromQueue(Guid.NewGuid());

        Assert.Equal(404, ((IStatusCodeHttpResult)result).StatusCode);
    }

    // ── JoinQueue ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task JoinQueue_Returns400_WhenQueueHasFiveOrMoreSessions()
    {
        var controller = CreateController(new StubChatService(queueSize: 5));
        var request = new SupportChatJoinQueueRequest("Test User", "test@example.com");

        var result = await controller.JoinQueue(request);

        Assert.Equal(400, ((IStatusCodeHttpResult)result).StatusCode);
    }

    [Fact]
    public async Task JoinQueue_Returns200_WhenQueueHasFewerThanFiveSessions()
    {
        var controller = CreateController(new StubChatService(queueSize: 4));
        var request = new SupportChatJoinQueueRequest("Test User", "test@example.com");

        var result = await controller.JoinQueue(request);

        Assert.Equal(200, ((IStatusCodeHttpResult)result).StatusCode);
    }
}
