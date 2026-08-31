using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Api.Controllers;

[Authorize, ApiController]
public sealed class ExportsController(IExportService exports) : ControllerBase
{
    private Guid? OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) && id != Guid.Empty ? id : null;
    private Guid? UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id != Guid.Empty ? id : null;

    [HttpPost("/exports")]
    public async Task<IActionResult> Create([FromBody] ExportRequest request)
    {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        try
        {
            return Ok(new { ok = true, job = await exports.RequestAsync(organizationId, UserId, request) });
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { ok = false, code = exception.Message });
        }
    }

    [HttpGet("/exports")]
    public async Task<IActionResult> List()
    {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        return Ok(new { ok = true, data = await exports.ListAsync(organizationId) });
    }

    [HttpGet("/exports/{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        var job = await exports.GetAsync(organizationId, id);
        return job is null ? NotFound() : Ok(new { ok = true, job });
    }

    [HttpGet("/exports/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        var job = await exports.GetAsync(organizationId, id);
        return job?.ResultPayload is null
            ? NotFound()
            : File(System.Text.Encoding.UTF8.GetBytes(job.ResultPayload), job.ResultMimeType ?? "text/plain", job.ResultFileName ?? "export.txt");
    }

    private ObjectResult OrganizationRequired() => StatusCode(StatusCodes.Status403Forbidden, new
    {
        ok = false,
        code = "ORGANIZATION_REQUIRED",
        message = "Selecione uma organização para continuar."
    });
}
