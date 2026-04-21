using JobFlow.Business.DI;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Business.Services;

[ScopedService]
public class SupportChatService : ISupportChatService
{
    private const int DefaultMinutesPerSession = 5;
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly IRepository<SupportChatSession> _sessions;
    private readonly IRepository<SupportChatMessage> _messages;
    private readonly IRepository<User> _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironmentAccessor _envAccessor;

    public SupportChatService(IUnitOfWork unitOfWork, IWebHostEnvironmentAccessor envAccessor)
    {
        _unitOfWork = unitOfWork;
        _envAccessor = envAccessor;
        _sessions = unitOfWork.RepositoryOf<SupportChatSession>();
        _messages = unitOfWork.RepositoryOf<SupportChatMessage>();
        _users = unitOfWork.RepositoryOf<User>();
    }

    public async Task<Result<SupportChatJoinQueueResponse>> JoinQueueAsync(
        string customerName, string customerEmail, Guid? customerId = null)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            return Result.Failure<SupportChatJoinQueueResponse>(
                Error.Validation("SupportChat.NameRequired", "Customer name is required."));

        if (string.IsNullOrWhiteSpace(customerEmail))
            return Result.Failure<SupportChatJoinQueueResponse>(
                Error.Validation("SupportChat.EmailRequired", "Customer email is required."));

        var queueCount = await _sessions.Query()
            .CountAsync(s => s.Status == SupportChatSessionStatus.Queued);

        var position = queueCount + 1;
        var waitSeconds = CalculateEstimatedWait(position);

        var session = new SupportChatSession
        {
            Id = Guid.NewGuid(),
            CustomerName = customerName.Trim(),
            CustomerEmail = customerEmail.Trim().ToLowerInvariant(),
            CustomerId = customerId,
            Status = SupportChatSessionStatus.Queued,
            EstimatedWaitSeconds = waitSeconds,
            CreatedAt = DateTime.UtcNow
        };

        await _sessions.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(new SupportChatJoinQueueResponse(session.Id, position, waitSeconds));
    }

    public async Task<Result<SupportChatSessionDto>> PickNextCustomerAsync(Guid repId, string repName)
    {
        var next = await _sessions.Query()
            .Where(s => s.Status == SupportChatSessionStatus.Queued)
            .OrderBy(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (next is null)
            return Result.Failure<SupportChatSessionDto>(
                Error.NotFound("SupportChat.QueueEmpty", "No customers are waiting in the queue."));

        return await AssignSession(next, repId, repName);
    }

    public async Task<Result<SupportChatSessionDto>> PickCustomerAsync(Guid sessionId, Guid repId, string repName)
    {
        var session = await _sessions.Query()
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session is null)
            return Result.Failure<SupportChatSessionDto>(
                Error.NotFound("SupportChat.SessionNotFound", "Session not found."));

        if (session.Status != SupportChatSessionStatus.Queued)
            return Result.Failure<SupportChatSessionDto>(
                Error.Conflict("SupportChat.NotQueued", "Session is not in the queue."));

        return await AssignSession(session, repId, repName);
    }

    public async Task<Result<SupportChatMessageDto>> SendMessageAsync(SupportChatSendMessageRequest request)
    {
        var session = await _sessions.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId);

        if (session is null)
            return Result.Failure<SupportChatMessageDto>(
                Error.NotFound("SupportChat.SessionNotFound", "Session not found."));

        if (session.Status == SupportChatSessionStatus.Closed)
            return Result.Failure<SupportChatMessageDto>(
                Error.Conflict("SupportChat.SessionClosed", "Cannot send messages to a closed session."));

        var message = new SupportChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = request.SessionId,
            SenderId = request.SenderId,
            SenderName = request.SenderName,
            SenderRole = request.SenderRole,
            Content = request.Content,
            FileUrl = request.FileUrl,
            FileName = request.FileName,
            FileSize = request.FileSize,
            SentAt = DateTime.UtcNow,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _messages.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(MapToMessageDto(message));
    }

    public async Task<Result<List<SupportChatQueueItemDto>>> GetQueueAsync()
    {
        var queued = await _sessions.Query()
            .AsNoTracking()
            .Where(s => s.Status == SupportChatSessionStatus.Queued)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        var items = queued.Select((s, i) => new SupportChatQueueItemDto(
            s.Id,
            s.CustomerName,
            s.CustomerEmail,
            i + 1,
            CalculateEstimatedWait(i + 1),
            s.CreatedAt)).ToList();

        return Result.Success(items);
    }

    public async Task<Result<List<SupportChatMessageDto>>> GetSessionMessagesAsync(Guid sessionId)
    {
        var messages = await _messages.Query()
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return Result.Success(messages.Select(MapToMessageDto).ToList());
    }

    public async Task<Result> CloseSessionAsync(Guid sessionId)
    {
        var session = await _sessions.Query()
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session is null)
            return Result.Failure(Error.NotFound("SupportChat.SessionNotFound", "Session not found."));

        session.Status = SupportChatSessionStatus.Closed;
        session.ClosedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<SupportChatFileUploadResponse>> UploadFileAsync(
        Stream stream, string fileName, string contentType)
    {
        if (stream.Length > MaxFileSizeBytes)
            return Result.Failure<SupportChatFileUploadResponse>(
                Error.Validation("SupportChat.FileTooLarge", "File exceeds the 10 MB limit."));

        var uploadsDir = Path.Combine(_envAccessor.WebRootPath, "uploads", "support");
        Directory.CreateDirectory(uploadsDir);

        var safeFileName = $"{Guid.NewGuid()}_{SanitizeFileName(fileName)}";
        var filePath = Path.Combine(uploadsDir, safeFileName);

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream);

        var fileUrl = $"/uploads/support/{safeFileName}";
        return Result.Success(new SupportChatFileUploadResponse(fileUrl, fileName, stream.Length));
    }

    public async Task<Result<SupportChatValidateCustomerResponse>> ValidateCustomerAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Success(new SupportChatValidateCustomerResponse(false, null, null, "Email is required."));

        var user = await _users.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant());

        if (user is not null)
            return Result.Success(new SupportChatValidateCustomerResponse(true, user.Id, user.Email, null));

        // Allow guest access — customer does not need to be a registered user
        return Result.Success(new SupportChatValidateCustomerResponse(true, null, email, null));
    }

    public async Task<Result<SupportChatSessionDto>> GetSessionAsync(Guid sessionId)
    {
        var session = await _sessions.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session is null)
            return Result.Failure<SupportChatSessionDto>(
                Error.NotFound("SupportChat.SessionNotFound", "Session not found."));

        var position = session.Status == SupportChatSessionStatus.Queued
            ? await _sessions.Query()
                .CountAsync(s => s.Status == SupportChatSessionStatus.Queued && s.CreatedAt <= session.CreatedAt)
            : 0;

        return Result.Success(MapToSessionDto(session, position));
    }

    public async Task<Result> RemoveFromQueueAsync(Guid sessionId)
    {
        var session = await _sessions.Query()
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Status == SupportChatSessionStatus.Queued);

        if (session is null)
            return Result.Failure(Error.NotFound("SupportChat.SessionNotFound", "Session not found or not in queue."));

        session.Status = SupportChatSessionStatus.Closed;
        session.ClosedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    private async Task<Result<SupportChatSessionDto>> AssignSession(
        SupportChatSession session, Guid repId, string repName)
    {
        session.Status = SupportChatSessionStatus.Active;
        session.AssignedRepId = repId;
        session.AssignedRepName = repName;
        session.StartedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        return Result.Success(MapToSessionDto(session, 0));
    }

    private static int CalculateEstimatedWait(int position)
        => position * DefaultMinutesPerSession * 60;

    private static SupportChatSessionDto MapToSessionDto(SupportChatSession s, int position) =>
        new(s.Id, s.CustomerName, s.CustomerEmail, s.CustomerId,
            s.AssignedRepId, s.AssignedRepName, s.Status,
            s.CreatedAt, s.StartedAt, s.ClosedAt,
            s.EstimatedWaitSeconds, position);

    private static SupportChatMessageDto MapToMessageDto(SupportChatMessage m) =>
        new(m.Id, m.SessionId, m.SenderId, m.SenderName, m.SenderRole,
            m.Content, m.FileUrl, m.FileName, m.FileSize, m.SentAt, m.IsRead);

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = string.Concat(fileName.Select(c => invalid.Contains(c) ? '_' : c));
        return safe.Length > 200 ? safe[..200] : safe;
    }
}
