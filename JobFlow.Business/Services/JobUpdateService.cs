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
public class JobUpdateService : IJobUpdateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateJobUpdateRequest> _validator;
    private readonly IRepository<Job> _jobs;
    private readonly IRepository<JobUpdate> _updates;
    private readonly IRepository<JobUpdateAttachment> _attachments;

    public JobUpdateService(IUnitOfWork unitOfWork, IValidator<CreateJobUpdateRequest> validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _jobs = unitOfWork.RepositoryOf<Job>();
        _updates = unitOfWork.RepositoryOf<JobUpdate>();
        _attachments = unitOfWork.RepositoryOf<JobUpdateAttachment>();
    }

    public async Task<Result<JobUpdateDto>> CreateAsync(
        Guid jobId,
        Guid organizationId,
        CreateJobUpdateRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Result.Failure<JobUpdateDto>(Error.Validation(
                "JobUpdate.ValidationFailed",
                string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
        }

        var job = await _jobs.Query()
            .Include(j => j.OrganizationClient)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job is null)
            return Result.Failure<JobUpdateDto>(JobUpdateErrors.JobNotFound);

        if (job.OrganizationClient.OrganizationId != organizationId)
            return Result.Failure<JobUpdateDto>(JobUpdateErrors.UnauthorizedJobAccess);

        var update = new JobUpdate
        {
            JobId = job.Id,
            OrganizationId = job.OrganizationClient.OrganizationId,
            OrganizationClientId = job.OrganizationClientId,
            Type = request.Type,
            Message = request.Message?.Trim(),
            Status = request.Status,
            OccurredAt = DateTimeOffset.UtcNow
        };

        if (request.Type == JobUpdateType.Photo && request.Attachments.Count > 0)
        {
            foreach (var attachment in request.Attachments)
            {
                update.Attachments.Add(new JobUpdateAttachment
                {
                    FileName = attachment.FileName,
                    ContentType = attachment.ContentType,
                    FileSizeBytes = attachment.SizeBytes,
                    FileData = attachment.Content
                });
            }
        }

        if (request.Type == JobUpdateType.StatusChange && request.Status.HasValue)
        {
            job.LifecycleStatus = request.Status.Value;
            _jobs.Update(job);
        }

        await _updates.AddAsync(update);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(ToDto(update));
    }

    public async Task<Result<IReadOnlyList<JobUpdateDto>>> GetByJobAsync(Guid jobId, Guid organizationId)
    {
        var job = await _jobs.Query()
            .Include(j => j.OrganizationClient)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job is null)
            return Result.Failure<IReadOnlyList<JobUpdateDto>>(JobUpdateErrors.JobNotFound);

        if (job.OrganizationClient.OrganizationId != organizationId)
            return Result.Failure<IReadOnlyList<JobUpdateDto>>(JobUpdateErrors.UnauthorizedJobAccess);

        var updates = await _updates.Query()
            .Where(u => u.JobId == jobId)
            .Include(u => u.Attachments)
            .OrderByDescending(u => u.OccurredAt)
            .ToListAsync();

        return Result.Success<IReadOnlyList<JobUpdateDto>>(updates.Select(ToDto).ToList());
    }

    public async Task<Result<IReadOnlyList<JobUpdateDto>>> GetByJobForClientAsync(
        Guid jobId,
        Guid organizationId,
        Guid organizationClientId)
    {
        var job = await _jobs.Query()
            .Include(j => j.OrganizationClient)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job is null)
            return Result.Failure<IReadOnlyList<JobUpdateDto>>(JobUpdateErrors.JobNotFound);

        if (job.OrganizationClient.OrganizationId != organizationId || job.OrganizationClientId != organizationClientId)
            return Result.Failure<IReadOnlyList<JobUpdateDto>>(JobUpdateErrors.UnauthorizedJobAccess);

        var updates = await _updates.Query()
            .Where(u => u.JobId == jobId)
            .Include(u => u.Attachments)
            .OrderByDescending(u => u.OccurredAt)
            .ToListAsync();

        return Result.Success<IReadOnlyList<JobUpdateDto>>(updates.Select(ToDto).ToList());
    }

    public async Task<Result<JobUpdateAttachmentDownloadDto>> GetAttachmentAsync(
        Guid jobId,
        Guid updateId,
        Guid attachmentId,
        Guid organizationId,
        Guid? organizationClientId = null)
    {
        var job = await _jobs.Query()
            .Include(j => j.OrganizationClient)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job is null)
            return Result.Failure<JobUpdateAttachmentDownloadDto>(JobUpdateErrors.JobNotFound);

        var authorized = job.OrganizationClient.OrganizationId == organizationId
            && (!organizationClientId.HasValue || job.OrganizationClientId == organizationClientId.Value);

        if (!authorized)
            return Result.Failure<JobUpdateAttachmentDownloadDto>(JobUpdateErrors.UnauthorizedJobAccess);

        var update = await _updates.Query()
            .FirstOrDefaultAsync(u => u.Id == updateId && u.JobId == jobId);

        if (update is null)
            return Result.Failure<JobUpdateAttachmentDownloadDto>(JobUpdateErrors.UpdateNotFound);

        var attachment = await _attachments.Query()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.JobUpdateId == updateId);

        if (attachment is null)
            return Result.Failure<JobUpdateAttachmentDownloadDto>(JobUpdateErrors.AttachmentNotFound);

        return Result.Success(new JobUpdateAttachmentDownloadDto(
            attachment.FileName,
            attachment.ContentType,
            attachment.FileData));
    }

    private static JobUpdateDto ToDto(JobUpdate update)
    {
        return new JobUpdateDto(
            update.Id,
            update.JobId,
            update.Type,
            update.Message,
            update.Status,
            update.OccurredAt,
            update.Attachments.Select(a => new JobUpdateAttachmentDto(
                a.Id,
                a.FileName,
                a.ContentType,
                a.FileSizeBytes)).ToList());
    }
}
