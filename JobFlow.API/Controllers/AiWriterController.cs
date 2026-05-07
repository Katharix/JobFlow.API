using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiWriterController : ControllerBase
{
    private readonly IAiWriterService _aiWriterService;

    public AiWriterController(IAiWriterService aiWriterService)
    {
        _aiWriterService = aiWriterService;
    }

    [HttpPost("estimate-draft")]
    [EnableRateLimiting("ai-writer")]
    public async Task<IResult> DraftEstimateNotes([FromBody] DraftEstimateNotesRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _aiWriterService.DraftEstimateNotesAsync(organizationId, request.LineItemNames);

        return result.IsSuccess
            ? Results.Ok(new { notes = result.Value })
            : result.ToProblemDetails();
    }

    [HttpPost("invoice-notes")]
    [EnableRateLimiting("ai-writer")]
    public async Task<IResult> DraftInvoiceNotes([FromBody] DraftInvoiceNotesRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _aiWriterService.DraftInvoiceNotesAsync(organizationId, request.LineItemDescriptions);

        return result.IsSuccess
            ? Results.Ok(new { notes = result.Value })
            : result.ToProblemDetails();
    }

    [HttpPost("job-summary")]
    [EnableRateLimiting("ai-writer")]
    public async Task<IResult> DraftJobSummary([FromBody] DraftJobSummaryRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _aiWriterService.DraftJobSummaryAsync(organizationId, request.JobTitle, request.ServiceNames);

        return result.IsSuccess
            ? Results.Ok(new { summary = result.Value })
            : result.ToProblemDetails();
    }
}

public record DraftEstimateNotesRequest(string[] LineItemNames);
public record DraftInvoiceNotesRequest(string[] LineItemDescriptions);
public record DraftJobSummaryRequest(string JobTitle, string[] ServiceNames);
