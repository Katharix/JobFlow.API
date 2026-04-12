using JobFlow.API.Extensions;
using JobFlow.API.Mappings;
using JobFlow.API.Services;
using JobFlow.Business;
using JobFlow.API.Models;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Hangfire;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobFlow.Infrastructure.Persistence;

namespace JobFlow.API.Controllers;

[Route("api/organization/clients/")]
[ApiController]
public class OrganizationClientController : ControllerBase
{
    private readonly IOrganizationClientService organizationClientService;
    private readonly IOrganizationClientPortalService _clientPortal;
    private readonly IMapper _mapper;
    private readonly ClientImportCsvService _csvImportService;
    private readonly ClientImportUploadSessionService _uploadSessionService;
    private readonly IDbContextFactory<JobFlowDbContext> _dbContextFactory;

    public OrganizationClientController(
        IOrganizationClientService organizationClientService,
        IOrganizationClientPortalService clientPortal,
        IMapper mapper,
        ClientImportCsvService csvImportService,
        ClientImportUploadSessionService uploadSessionService,
        IDbContextFactory<JobFlowDbContext> dbContextFactory)
    {
        this.organizationClientService = organizationClientService;
        _clientPortal = clientPortal;
        _mapper = mapper;
        _csvImportService = csvImportService;
        _uploadSessionService = uploadSessionService;
        _dbContextFactory = dbContextFactory;
    }

    [HttpGet]
    [Route("all")]
    public async Task<IResult> GetAllClients()
    {
        var result = await organizationClientService.GetAllClients();
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("orgall")]
    public async Task<IResult> GetAllClientsByOrganizationId(
        [FromQuery] string? cursor = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] bool missingEmailOnly = false,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var hasFilters = missingEmailOnly
            || !string.IsNullOrWhiteSpace(search)
            || !string.IsNullOrWhiteSpace(sortBy)
            || !string.IsNullOrWhiteSpace(sortDirection);

        if (!pageSize.HasValue && string.IsNullOrWhiteSpace(cursor) && !hasFilters)
        {
            var legacyResult = await organizationClientService.GetAllClientsByOrganizationId(organizationId);
            return legacyResult.IsSuccess
                ? Results.Ok(legacyResult.Value)
                : legacyResult.ToProblemDetails();
        }

        var result = await organizationClientService.GetClientsByOrganizationPagedAsync(
            organizationId,
            Math.Clamp(pageSize ?? 50, 1, 100),
            cursor,
            missingEmailOnly,
            search,
            sortBy,
            sortDirection);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }


    [HttpDelete]
    [Route("delete")]
    public async Task<IResult> DeleteClient(Guid clientId)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await organizationClientService.DeleteClient(clientId, organizationId);
        return result.IsSuccess ? Results.Ok(result) : result.ToProblemDetails();
    }

    [HttpPost("upsert")]
    public async Task<IResult> UpsertClient(
        [FromBody] OrganizationClientDto model)
    {
        var organizationId = HttpContext.GetOrganizationId();
        if (organizationId == Guid.Empty)
            return Results.BadRequest("OrganizationId is required.");

        model.Organization = null;
        model.OrganizationId = organizationId;
        var entity = _mapper.Map<OrganizationClient>(model);

        var result = await organizationClientService.UpsertClient(entity);

        if (!result.IsSuccess)
            return result.ToProblemDetails();

        var responseDto = _mapper.Map<OrganizationClientDto>(result.Value);
        responseDto.Organization = null;

        return Results.Ok(Result.Success(responseDto));
    }


    [HttpPost]
    [Route("upsert/multi")]
    public async Task<IResult> UpsertMultipleClients(
        [FromBody] IEnumerable<OrganizationClientDto> modelList)
    {
        var organizationId = HttpContext.GetOrganizationId();
        if (organizationId == Guid.Empty)
            return Results.BadRequest("OrganizationId is required.");

        var entities = modelList.Select(dto =>
        {
            dto.Organization = null;
            dto.OrganizationId = organizationId;
            return _mapper.Map<OrganizationClient>(dto);
        });

        var result = await organizationClientService.UpsertMultipleClients(entities);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{organizationClientId:guid}/send-client-hub-link")]
    public async Task<IResult> SendClientHubLink(Guid organizationClientId)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var clientResult = await organizationClientService.GetClientById(organizationClientId);
        if (!clientResult.IsSuccess)
            return clientResult.ToProblemDetails();

        if (clientResult.Value.OrganizationId != organizationId)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(clientResult.Value.EmailAddress))
            return Results.BadRequest("Client email address is required.");

        var result = await _clientPortal.SendMagicLinkWithUrlAsync(
            organizationId,
            organizationClientId,
            clientResult.Value.EmailAddress);

        return result.IsSuccess
            ? Results.Ok(new { magicLink = result.Value })
            : result.ToProblemDetails();
    }

    [HttpPost]
    [Route("restore")]
    public async Task<IResult> RestoreClient(Guid clientId)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await organizationClientService.RestoreClient(clientId, organizationId);
        return result.IsSuccess ? Results.Ok(result) : result.ToProblemDetails();
    }

    [HttpPost("import/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IResult> PreviewClientImport([FromForm] PreviewClientImportRequest request, CancellationToken cancellationToken)
    {
        var file = request.File;
        if (file is null)
            return Results.BadRequest("A CSV file is required.");

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest("Only CSV files are supported in this version.");

        try
        {
            var organizationId = HttpContext.GetOrganizationId();
            var parsed = await _csvImportService.ParseAsync(file, cancellationToken);
            var source = string.IsNullOrWhiteSpace(request.SourceSystem) ? "csv" : request.SourceSystem.Trim();
            var uploadSessionId = await _uploadSessionService.SaveAsync(organizationId, source, parsed.Rows, cancellationToken);

            var previewRows = parsed.Rows
                .Take(25)
                .Select(r => r.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var response = new ClientImportPreviewResponse
            {
                UploadToken = uploadSessionId.ToString("N"),
                SourceSystem = source,
                SourceColumns = parsed.Headers,
                SuggestedMappings = parsed.SuggestedMappings,
                PreviewRows = previewRows,
                TotalRows = parsed.Rows.Count
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    [HttpPost("import/start")]
    public async Task<IResult> StartClientImport([FromBody] StartClientImportRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.UploadToken))
            return Results.BadRequest("Upload token is required.");

        if (!Guid.TryParse(request.UploadToken, out var uploadSessionId))
            return Results.BadRequest("Invalid upload token format.");

        var organizationId = HttpContext.GetOrganizationId();
        var sessionMeta = await _uploadSessionService.ValidateSessionAsync(uploadSessionId, organizationId, CancellationToken.None);
        if (sessionMeta is null)
            return Results.BadRequest("Import session expired or invalid. Please upload your CSV again.");

        if (request.ColumnMappings.Count == 0)
            return Results.BadRequest("At least one column mapping is required.");

        var jobId = Guid.NewGuid();
        var sourceSystem = string.IsNullOrWhiteSpace(request.SourceSystem) ? "csv" : request.SourceSystem.Trim();

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var importJob = new ClientImportJob
        {
            Id = jobId,
            OrganizationId = organizationId,
            SourceSystem = sourceSystem,
            Status = "queued",
            TotalRows = sessionMeta.TotalRows,
            ProcessedRows = 0,
            SucceededRows = 0,
            FailedRows = 0,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        dbContext.Set<ClientImportJob>().Add(importJob);
        await dbContext.SaveChangesAsync();

        BackgroundJob.Enqueue<ClientImportProcessor>(
            processor => processor.ProcessAsync(jobId, organizationId, uploadSessionId, request.ColumnMappings));

        return Results.Ok(new StartClientImportResponse { JobId = jobId.ToString("N") });
    }

    [HttpGet("import/jobs/{jobId}")]
    public async Task<IResult> GetClientImportStatus(string jobId)
    {
        if (!Guid.TryParse(jobId, out var parsedJobId))
            return Results.BadRequest("Invalid import job id.");

        var organizationId = HttpContext.GetOrganizationId();

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var job = await dbContext.Set<ClientImportJob>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == parsedJobId && x.OrganizationId == organizationId);

        if (job is null)
            return Results.NotFound();

        var errors = await dbContext.Set<ClientImportJobError>()
            .AsNoTracking()
            .Where(x => x.ClientImportJobId == parsedJobId)
            .OrderBy(x => x.RowNumber)
            .Take(100)
            .Select(x => new ClientImportErrorItem
            {
                RowNumber = x.RowNumber,
                Message = x.Message
            })
            .ToListAsync();

        var status = new ClientImportJobStatusResponse
        {
            JobId = job.Id.ToString("N"),
            SourceSystem = job.SourceSystem,
            Status = job.Status,
            TotalRows = job.TotalRows,
            ProcessedRows = job.ProcessedRows,
            SucceededRows = job.SucceededRows,
            FailedRows = job.FailedRows,
            ErrorMessage = job.ErrorMessage,
            Errors = errors
        };

        return Results.Ok(status);
    }
}