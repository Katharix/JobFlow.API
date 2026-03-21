using JobFlow.API.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/invoicing-settings")]
public class InvoicingSettingsController : ControllerBase
{
    private readonly IInvoicingSettingsService _settings;

    public InvoicingSettingsController(IInvoicingSettingsService settings)
    {
        _settings = settings;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var organizationId = HttpContext.GetOrganizationId();

        var result = await _settings.GetInvoicingSettingsAsync(organizationId);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] InvoicingSettingsUpsertRequestDto dto)
    {
        var organizationId = HttpContext.GetOrganizationId();

        var result = await _settings.UpsertInvoicingSettingsAsync(organizationId, dto);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}