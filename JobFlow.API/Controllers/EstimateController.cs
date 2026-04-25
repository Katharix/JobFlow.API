using JobFlow.API.Extensions;
using JobFlow.Business;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/estimates")]
public class EstimateController : ControllerBase
{
    private readonly IEstimateService estimateService;

    public EstimateController(IEstimateService estimateService)
    {
        this.estimateService = estimateService;
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await estimateService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : ProblemFrom(result);
    }

    [HttpGet("organization")]
    [Authorize]
    public async Task<IActionResult> GetByOrganization()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await estimateService.GetByOrganizationAsync(organizationId);
        return result.IsSuccess ? Ok(result.Value) : ProblemFrom(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateEstimateRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();

        var result = await estimateService.CreateAsync(new CreateEstimateRequest(
            organizationId,
            request.OrganizationClientId,
            request.Title,
            request.Description,
            request.Notes,
            request.LineItems
        ));
        return result.IsSuccess ? Ok(result.Value) : ProblemFrom(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEstimateRequest request)
    {
        var result = await estimateService.UpdateAsync(id, request);
        return result.IsSuccess ? Ok(result.Value) : ProblemFrom(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await estimateService.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : ProblemFrom(result);
    }

    [HttpPost("{id:guid}/send")]
    [Authorize]
    public async Task<IActionResult> Send(Guid id, [FromBody] SendEstimateRequest request)
    {
        var result = await estimateService.SendAsync(id, request);
        return result.IsSuccess ? Ok(result.Value) : ProblemFrom(result);
    }

    [HttpGet("public/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublic(string token)
    {
        var result = await estimateService.GetByPublicTokenAsync(token);
        return result.IsSuccess ? Ok(result.Value) : ProblemFrom(result);
    }

    [HttpGet("public/{token}/pdf")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicPdf(string token)
    {
        var result = await estimateService.GetPublicPdfAsync(token);
        if (!result.IsSuccess)
            return ProblemFrom(result);

        Response.Headers.CacheControl = "no-store";
        return File(result.Value, "application/pdf", $"estimate-{token}.pdf");
    }

    [HttpPost("first-win")]
    [Authorize]
    public async Task<IActionResult> CreateFirstWin()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await estimateService.CreateFirstWinAsync(organizationId);
        return result.IsSuccess ? Ok(result.Value) : ProblemFrom(result);
    }

    private ObjectResult ProblemFrom(Result result)
    {
        var problem = result.ToProblemDetails() as Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult;

        if (problem == null)
            return Problem(statusCode: 500);

        return new ObjectResult(problem.ProblemDetails)
        {
            StatusCode = problem.StatusCode
        };
    }
}