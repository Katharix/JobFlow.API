using FluentValidation;
using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Business.Services;

[ScopedService]
public class EstimateRevisionService : IEstimateRevisionService
{
    private static readonly EstimateRevisionStatus[] OpenStatuses =
    [
        EstimateRevisionStatus.Requested,
        EstimateRevisionStatus.InReview
    ];

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IValidator<CreateEstimateRevisionRequest> _validator;
    private readonly IRepository<Estimate> _estimates;
    private readonly IRepository<EstimateRevisionRequest> _revisionRequests;
    private readonly IRepository<EstimateRevisionAttachment> _attachments;
    private readonly IRepository<Organization> _organizations;
    private readonly IRepository<OrganizationClient> _clients;

    public EstimateRevisionService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IValidator<CreateEstimateRevisionRequest> validator)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _validator = validator;

        _estimates = unitOfWork.RepositoryOf<Estimate>();
        _revisionRequests = unitOfWork.RepositoryOf<EstimateRevisionRequest>();
        _attachments = unitOfWork.RepositoryOf<EstimateRevisionAttachment>();
        _organizations = unitOfWork.RepositoryOf<Organization>();
        _clients = unitOfWork.RepositoryOf<OrganizationClient>();
    }

    public async Task<Result<EstimateRevisionRequestDto>> CreateAsync(
        Guid estimateId,
        Guid organizationId,
        Guid organizationClientId,
        CreateEstimateRevisionRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Result.Failure<EstimateRevisionRequestDto>(
                Error.Validation("EstimateRevision.ValidationFailed", string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));

        var estimate = await _estimates.Query()
            .FirstOrDefaultAsync(x => x.Id == estimateId);

        if (estimate is null)
            return Result.Failure<EstimateRevisionRequestDto>(EstimateRevisionErrors.EstimateNotFound);

        if (estimate.OrganizationId != organizationId || estimate.OrganizationClientId != organizationClientId)
            return Result.Failure<EstimateRevisionRequestDto>(EstimateRevisionErrors.UnauthorizedEstimateAccess);

        if (estimate.Status is not (EstimateStatus.Sent or EstimateStatus.Accepted))
            return Result.Failure<EstimateRevisionRequestDto>(EstimateRevisionErrors.InvalidEstimateStatus);

        var hasOpenRevision = await _revisionRequests.Query()
            .AnyAsync(x => x.EstimateId == estimateId && OpenStatuses.Contains(x.Status));

        if (hasOpenRevision)
            return Result.Failure<EstimateRevisionRequestDto>(EstimateRevisionErrors.OpenRevisionAlreadyExists);

        var nextRevisionNumber = (await _revisionRequests.Query()
            .Where(x => x.EstimateId == estimateId)
            .Select(x => (int?)x.RevisionNumber)
            .MaxAsync() ?? 0) + 1;

        var revisionRequest = new EstimateRevisionRequest
        {
            EstimateId = estimateId,
            OrganizationId = organizationId,
            OrganizationClientId = organizationClientId,
            RevisionNumber = nextRevisionNumber,
            Status = EstimateRevisionStatus.Requested,
            RequestMessage = request.Message.Trim(),
            RequestedAt = DateTimeOffset.UtcNow
        };

        if (request.Attachments.Count > 0)
        {
            foreach (var attachment in request.Attachments)
            {
                revisionRequest.Attachments.Add(new EstimateRevisionAttachment
                {
                    FileName = attachment.FileName,
                    ContentType = attachment.ContentType,
                    FileSizeBytes = attachment.SizeBytes,
                    FileData = attachment.Content
                });
            }
        }

        await _revisionRequests.AddAsync(revisionRequest);

        estimate.Status = EstimateStatus.RevisionRequested;
        estimate.UpdatedAt = DateTimeOffset.UtcNow;
        _estimates.Update(estimate);

        await _unitOfWork.SaveChangesAsync();

        var organization = await _organizations.Query()
            .FirstOrDefaultAsync(x => x.Id == organizationId);
        if (organization is null)
            return Result.Failure<EstimateRevisionRequestDto>(EstimateRevisionErrors.OrganizationNotFound);

        var client = await _clients.Query()
            .FirstOrDefaultAsync(x => x.Id == organizationClientId && x.OrganizationId == organizationId);
        if (client is null)
            return Result.Failure<EstimateRevisionRequestDto>(EstimateRevisionErrors.ClientNotFound);

        await _notificationService.SendOrganizationEstimateRevisionRequestedNotificationAsync(
            organization,
            client,
            estimate,
            revisionRequest.RequestMessage);

        return Result.Success(ToDto(revisionRequest));
    }

    public async Task<Result<IReadOnlyList<EstimateRevisionRequestDto>>> GetByEstimateAsync(
        Guid estimateId,
        Guid organizationId,
        Guid organizationClientId)
    {
        var estimate = await _estimates.Query()
            .FirstOrDefaultAsync(x => x.Id == estimateId);

        if (estimate is null)
            return Result.Failure<IReadOnlyList<EstimateRevisionRequestDto>>(EstimateRevisionErrors.EstimateNotFound);

        if (estimate.OrganizationId != organizationId || estimate.OrganizationClientId != organizationClientId)
            return Result.Failure<IReadOnlyList<EstimateRevisionRequestDto>>(EstimateRevisionErrors.UnauthorizedEstimateAccess);

        var revisions = await _revisionRequests.Query()
            .Where(x => x.EstimateId == estimateId)
            .Include(x => x.Attachments)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync();

        return Result.Success<IReadOnlyList<EstimateRevisionRequestDto>>(revisions.Select(ToDto).ToList());
    }

    public async Task<Result<EstimateRevisionAttachmentDownloadDto>> GetAttachmentAsync(
        Guid estimateId,
        Guid revisionRequestId,
        Guid attachmentId,
        Guid organizationId,
        Guid organizationClientId)
    {
        var estimate = await _estimates.Query()
            .FirstOrDefaultAsync(x => x.Id == estimateId);

        if (estimate is null)
            return Result.Failure<EstimateRevisionAttachmentDownloadDto>(EstimateRevisionErrors.EstimateNotFound);

        if (estimate.OrganizationId != organizationId || estimate.OrganizationClientId != organizationClientId)
            return Result.Failure<EstimateRevisionAttachmentDownloadDto>(EstimateRevisionErrors.UnauthorizedEstimateAccess);

        var revision = await _revisionRequests.Query()
            .FirstOrDefaultAsync(x => x.Id == revisionRequestId && x.EstimateId == estimateId);

        if (revision is null)
            return Result.Failure<EstimateRevisionAttachmentDownloadDto>(EstimateRevisionErrors.RevisionRequestNotFound);

        var attachment = await _attachments.Query()
            .FirstOrDefaultAsync(x => x.Id == attachmentId && x.EstimateRevisionRequestId == revisionRequestId);

        if (attachment is null)
            return Result.Failure<EstimateRevisionAttachmentDownloadDto>(EstimateRevisionErrors.AttachmentNotFound);

        return Result.Success(new EstimateRevisionAttachmentDownloadDto(
            attachment.FileName,
            attachment.ContentType,
            attachment.FileData));
    }

    private static EstimateRevisionRequestDto ToDto(EstimateRevisionRequest request)
    {
        return new EstimateRevisionRequestDto(
            request.Id,
            request.EstimateId,
            request.OrganizationId,
            request.OrganizationClientId,
            request.RevisionNumber,
            request.Status,
            request.RequestMessage,
            request.OrganizationResponseMessage,
            request.RequestedAt,
            request.ReviewedAt,
            request.ResolvedAt,
            request.Attachments.Select(x => new EstimateRevisionAttachmentDto(
                x.Id,
                x.FileName,
                x.ContentType,
                x.FileSizeBytes)).ToList());
    }
}
