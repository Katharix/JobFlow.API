using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.API.Services;

public sealed class DataExportJobProcessor
{
    private readonly IDbContextFactory<JobFlowDbContext> _dbContextFactory;
    private readonly DataExportBuilderService _builder;
    private readonly ILogger<DataExportJobProcessor> _logger;

    public DataExportJobProcessor(
        IDbContextFactory<JobFlowDbContext> dbContextFactory,
        DataExportBuilderService builder,
        ILogger<DataExportJobProcessor> logger)
    {
        _dbContextFactory = dbContextFactory;
        _builder = builder;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid jobId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var job = await dbContext.Set<DataExportJob>()
            .FirstOrDefaultAsync(x => x.Id == jobId && x.OrganizationId == organizationId);

        if (job is null)
        {
            _logger.LogWarning("Data export job not found. JobId={JobId}, OrganizationId={OrganizationId}", jobId, organizationId);
            return;
        }

        if (job.Status is "completed" or "running")
        {
            return;
        }

        try
        {
            job.Status = "running";
            job.StartedAtUtc = DateTime.UtcNow;
            job.ErrorMessage = null;
            await dbContext.SaveChangesAsync();

            var (zipContent, zipName) = await _builder.BuildZipPackageAsync(organizationId, CancellationToken.None);

            job.Status = "completed";
            job.FileContent = zipContent;
            job.FileName = zipName;
            job.ContentType = "application/zip";
            job.CompletedAtUtc = DateTime.UtcNow;
            job.ExpiresAtUtc = DateTime.UtcNow.AddDays(7);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process data export job. JobId={JobId}, OrganizationId={OrganizationId}", jobId, organizationId);

            job.Status = "failed";
            job.ErrorMessage = ex.Message;
            job.CompletedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }
}