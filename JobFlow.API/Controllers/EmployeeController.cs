using JobFlow.API.Extensions;
using JobFlow.API.Models;
using JobFlow.API.Services;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeesController> _logger;
    private readonly EmployeeImportCsvService _csvImportService;
    private readonly EmployeeImportUploadSessionService _uploadSessionService;
    private readonly IDbContextFactory<JobFlowDbContext> _dbContextFactory;

    public EmployeesController(
        IEmployeeService employeeService,
        ILogger<EmployeesController> logger,
        EmployeeImportCsvService csvImportService,
        EmployeeImportUploadSessionService uploadSessionService,
        IDbContextFactory<JobFlowDbContext> dbContextFactory)
    {
        _employeeService = employeeService;
        _logger = logger;
        _csvImportService = csvImportService;
        _uploadSessionService = uploadSessionService;
        _dbContextFactory = dbContextFactory;
    }

    [HttpGet("organization")]
    public async Task<IResult> GetByOrganizationId()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _employeeService.GetByOrganizationIdAsync(organizationId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id}")]
    public async Task<IResult> GetById(Guid id)
    {
        var result = await _employeeService.GetByIdAsync(id);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    public async Task<IResult> Create(CreateEmployeeRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        request.OrganizationId = organizationId;
        var result = await _employeeService.CreateAsync(request);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("bulk")]
    public async Task<IResult> BulkCreate([FromBody] List<CreateEmployeeRequest> requests)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _employeeService.BulkCreateAsync(organizationId, requests);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id}")]
    public async Task<IResult> Update(Guid id, UpdateEmployeeRequest request)
    {
        var result = await _employeeService.UpdateAsync(id, request);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpDelete("{id}")]
    public async Task<IResult> Delete(Guid id)
    {
        var result = await _employeeService.DeleteAsync(id);
        return result.IsSuccess ? Results.Ok(result) : result.ToProblemDetails();
    }

    [HttpGet("email/{email}")]
    public async Task<IResult> EmployeeEmailExist(string email)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _employeeService.EmployeeExistByEmailAsync(organizationId, email);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("import/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IResult> PreviewEmployeeImport([FromForm] PreviewEmployeeImportRequest request, CancellationToken cancellationToken)
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

            var response = new EmployeeImportPreviewResponse
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
    public async Task<IResult> StartEmployeeImport([FromBody] StartEmployeeImportRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.UploadToken))
            return Results.BadRequest("Upload token is required.");

        if (!Guid.TryParse(request.UploadToken, out var uploadSessionId))
            return Results.BadRequest("Invalid upload token format.");

        var organizationId = HttpContext.GetOrganizationId();
        var uploadSession = await _uploadSessionService.GetActiveSessionAsync(uploadSessionId, organizationId, CancellationToken.None);
        if (uploadSession is null)
            return Results.BadRequest("Import session expired or invalid. Please upload your CSV again.");

        if (request.ColumnMappings.Count == 0)
            return Results.BadRequest("At least one column mapping is required.");

        var jobId = Guid.NewGuid();
        var sourceSystem = string.IsNullOrWhiteSpace(request.SourceSystem) ? "csv" : request.SourceSystem.Trim();

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var importJob = new EmployeeImportJob
        {
            Id = jobId,
            OrganizationId = organizationId,
            SourceSystem = sourceSystem,
            Status = "queued",
            TotalRows = uploadSession.TotalRows,
            ProcessedRows = 0,
            SucceededRows = 0,
            FailedRows = 0,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        dbContext.Set<EmployeeImportJob>().Add(importJob);
        await dbContext.SaveChangesAsync();

        BackgroundJob.Enqueue<EmployeeImportProcessor>(
            processor => processor.ProcessAsync(jobId, organizationId, uploadSessionId, request.ColumnMappings));

        return Results.Ok(new StartEmployeeImportResponse { JobId = jobId.ToString("N") });
    }

    [HttpGet("import/jobs/{jobId}")]
    public async Task<IResult> GetEmployeeImportStatus(string jobId)
    {
        if (!Guid.TryParse(jobId, out var parsedJobId))
            return Results.BadRequest("Invalid import job id.");

        var organizationId = HttpContext.GetOrganizationId();

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var job = await dbContext.Set<EmployeeImportJob>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == parsedJobId && x.OrganizationId == organizationId);

        if (job is null)
            return Results.NotFound();

        var errors = await dbContext.Set<EmployeeImportJobError>()
            .AsNoTracking()
            .Where(x => x.EmployeeImportJobId == parsedJobId)
            .OrderBy(x => x.RowNumber)
            .Take(100)
            .Select(x => new EmployeeImportErrorItem
            {
                RowNumber = x.RowNumber,
                Message = x.Message
            })
            .ToListAsync();

        var status = new EmployeeImportJobStatusResponse
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