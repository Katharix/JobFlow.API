using Hangfire;
using JobFlow.API.Extensions;
using JobFlow.API.Models;
using JobFlow.API.Services;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/data-export")]
public class DataExportController : ControllerBase
{
    private readonly IDbContextFactory<JobFlowDbContext> _dbContextFactory;
    private readonly DataExportBuilderService _builder;
    private readonly IOrganizationService _organizations;

    public DataExportController(
        IDbContextFactory<JobFlowDbContext> dbContextFactory,
        DataExportBuilderService builder,
        IOrganizationService organizations)
    {
        _dbContextFactory = dbContextFactory;
        _builder = builder;
        _organizations = organizations;
    }

    [HttpGet("json")]
    public async Task<IResult> ExportOrganizationDataJson(CancellationToken cancellationToken)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var (bytes, fileName) = await _builder.BuildJsonBundleAsync(organizationId, cancellationToken);

        return Results.File(bytes, "application/json", fileName);
    }

    [HttpGet("clients.csv")]
    public async Task<IResult> ExportClientsCsv(CancellationToken cancellationToken)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var (bytes, fileName) = await _builder.BuildClientsCsvAsync(organizationId, cancellationToken);

        return Results.File(bytes, "text/csv", fileName);
    }

    [HttpPost("jobs")]
    public async Task<IResult> StartDataExportJob(CancellationToken cancellationToken)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var userId = HttpContext.GetUserId();

        var orgResult = await _organizations.GetOrganizationDtoById(organizationId);
        if (orgResult.IsFailure)
        {
            return Results.Problem(statusCode: 404, title: "Organization not found", detail: "Organization context is invalid.");
        }

        if (!HasMinPlan(orgResult.Value.SubscriptionPlanName, "Flow"))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Subscription Required",
                detail: "A Flow plan is required for async ZIP data exports.");
        }

        var jobId = Guid.NewGuid();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var activeJob = await dbContext.Set<DataExportJob>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && (x.Status == "queued" || x.Status == "running"), cancellationToken);

        if (activeJob is not null)
        {
            return Results.Ok(new StartDataExportJobResponse { JobId = activeJob.Id.ToString("N") });
        }

        dbContext.Set<DataExportJob>().Add(new DataExportJob
        {
            Id = jobId,
            OrganizationId = organizationId,
            RequestedByUserId = userId,
            Status = "queued",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        BackgroundJob.Enqueue<DataExportJobProcessor>(x => x.ProcessAsync(jobId, organizationId));

        return Results.Ok(new StartDataExportJobResponse { JobId = jobId.ToString("N") });
    }

    [HttpGet("jobs")]
    public async Task<IResult> GetDataExportJobs(CancellationToken cancellationToken)
    {
        var organizationId = HttpContext.GetOrganizationId();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var jobs = await dbContext.Set<DataExportJob>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .Select(x => new DataExportJobStatusResponse
            {
                JobId = x.Id.ToString("N"),
                Status = x.Status,
                ErrorMessage = x.ErrorMessage,
                FileName = x.FileName,
                ContentType = x.ContentType,
                StartedAtUtc = x.StartedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc,
                ExpiresAtUtc = x.ExpiresAtUtc,
                DownloadCount = x.DownloadCount
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(jobs);
    }

    [HttpGet("jobs/{jobId}")]
    public async Task<IResult> GetDataExportJobStatus(string jobId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(jobId, out var parsedJobId))
            return Results.BadRequest("Invalid export job id.");

        var organizationId = HttpContext.GetOrganizationId();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var job = await dbContext.Set<DataExportJob>()
            .AsNoTracking()
            .Where(x => x.Id == parsedJobId && x.OrganizationId == organizationId)
            .Select(x => new DataExportJobStatusResponse
            {
                JobId = x.Id.ToString("N"),
                Status = x.Status,
                ErrorMessage = x.ErrorMessage,
                FileName = x.FileName,
                ContentType = x.ContentType,
                StartedAtUtc = x.StartedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc,
                ExpiresAtUtc = x.ExpiresAtUtc,
                DownloadCount = x.DownloadCount
            })
            .FirstOrDefaultAsync(cancellationToken);

        return job is null ? Results.NotFound() : Results.Ok(job);
    }

    [HttpGet("jobs/{jobId}/download")]
    public async Task<IResult> DownloadDataExportJobFile(string jobId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(jobId, out var parsedJobId))
            return Results.BadRequest("Invalid export job id.");

        var organizationId = HttpContext.GetOrganizationId();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var job = await dbContext.Set<DataExportJob>()
            .FirstOrDefaultAsync(x => x.Id == parsedJobId && x.OrganizationId == organizationId, cancellationToken);

        if (job is null)
            return Results.NotFound();

        if (job.Status != "completed" || job.FileContent is null || string.IsNullOrWhiteSpace(job.FileName))
            return Results.Conflict(new { message = "Export file is not ready yet." });

        if (job.ExpiresAtUtc.HasValue && job.ExpiresAtUtc.Value < DateTime.UtcNow)
            return Results.StatusCode(StatusCodes.Status410Gone);

        job.DownloadCount += 1;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.File(job.FileContent, job.ContentType ?? "application/octet-stream", job.FileName);
    }

    private static bool HasMinPlan(string? planName, string required)
    {
        static int Rank(string? plan)
        {
            var value = (plan ?? string.Empty).Trim().ToLowerInvariant();
            return value switch
            {
                "go" => 0,
                "flow" => 1,
                "max" => 2,
                _ => -1
            };
        }

        return Rank(planName) >= Rank(required);
    }
}
