using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/help-content")]
public class HelpContentController : ControllerBase
{
    private readonly IHelpContentService _helpContentService;

    public HelpContentController(IHelpContentService helpContentService)
    {
        _helpContentService = helpContentService;
    }

    // ── Public (any authenticated user) ───────────────────

    [HttpGet("articles/published")]
    [Authorize]
    public async Task<IResult> GetPublishedArticles()
    {
        var result = await _helpContentService.GetPublishedArticlesAsync();
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("changelog/published")]
    [Authorize]
    public async Task<IResult> GetPublishedChangelog()
    {
        var result = await _helpContentService.GetPublishedChangelogAsync();
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    // ── Admin (Katharix staff only) ───────────────────────

    [HttpGet("articles")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> GetAllArticles()
    {
        var result = await _helpContentService.GetAllArticlesAsync();
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("articles/{id:guid}")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> GetArticle([FromRoute] Guid id)
    {
        var result = await _helpContentService.GetArticleByIdAsync(id);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("articles")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> CreateArticle([FromBody] HelpArticleCreateRequest request)
    {
        var createdBy = HttpContext.GetFirebaseUid();
        var result = await _helpContentService.CreateArticleAsync(request, createdBy);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("articles")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> UpdateArticle([FromBody] HelpArticleUpdateRequest request)
    {
        var updatedBy = HttpContext.GetFirebaseUid();
        var result = await _helpContentService.UpdateArticleAsync(request, updatedBy);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpDelete("articles/{id:guid}")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> DeleteArticle([FromRoute] Guid id)
    {
        var result = await _helpContentService.DeleteArticleAsync(id);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    // ── Changelog Admin ───────────────────────────────────

    [HttpGet("changelog")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> GetAllChangelog()
    {
        var result = await _helpContentService.GetAllChangelogAsync();
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("changelog")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> CreateChangelogEntry([FromBody] ChangelogEntryCreateRequest request)
    {
        var createdBy = HttpContext.GetFirebaseUid();
        var result = await _helpContentService.CreateChangelogEntryAsync(request, createdBy);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("changelog")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> UpdateChangelogEntry([FromBody] ChangelogEntryUpdateRequest request)
    {
        var updatedBy = HttpContext.GetFirebaseUid();
        var result = await _helpContentService.UpdateChangelogEntryAsync(request, updatedBy);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpDelete("changelog/{id:guid}")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> DeleteChangelogEntry([FromRoute] Guid id)
    {
        var result = await _helpContentService.DeleteChangelogEntryAsync(id);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    // ── Seed ──────────────────────────────────────────────

    [HttpPost("seed")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin}")]
    public async Task<IResult> SeedHelpContent()
    {
        var createdBy = HttpContext.GetFirebaseUid();
        var result = await _helpContentService.SeedHelpContentAsync(createdBy);
        return result.IsSuccess ? Results.Ok(new { message = "Help content seeded successfully." }) : result.ToProblemDetails();
    }
}
