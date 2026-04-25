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
}

public record DraftEstimateNotesRequest(string[] LineItemNames);
