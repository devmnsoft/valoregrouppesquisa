using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Api.Controllers;

[Authorize, ApiController]
public sealed class ExportsController(IExportService exports, ICurrentRequestContext currentRequest) : ControllerBase {
    private CurrentRequestContext Context => currentRequest.GetCurrent();
    private Guid? OrganizationId => Context.EffectiveOrganizationId;
    private Guid? UserId => Context.UserId;

    [HttpPost("/exports")]
    public async Task<IActionResult> Create([FromBody] ExportRequest request, CancellationToken cancellationToken) {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        try {
            return Accepted(new { ok = true, job = await exports.RequestAsync(organizationId, UserId, request, HttpContext.TraceIdentifier, cancellationToken) });
        }
        catch (InvalidOperationException exception) {
            return StatusCode(StatusCodes.Status403Forbidden, new { ok = false, code = exception.Message });
        }
    }

    [HttpGet("/exports")]
    public async Task<IActionResult> List(CancellationToken cancellationToken) {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        return Ok(new { ok = true, data = await exports.ListAsync(organizationId, cancellationToken) });
    }

    [HttpGet("/exports/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        var job = await exports.GetAsync(organizationId, id, cancellationToken);
        return job is null ? NotFound() : Ok(new { ok = true, job });
    }

    [HttpGet("/exports/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken) {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        var job = await exports.GetAsync(organizationId, id, cancellationToken);
        return job?.ResultPayload is null || job.Status != "completed" || job.ExpiresAt <= DateTimeOffset.UtcNow
            ? NotFound()
            : File(Convert.FromBase64String(job.ResultPayload), job.ResultMimeType ?? "application/octet-stream", job.ResultFileName ?? "export.bin");
    }

    private ObjectResult OrganizationRequired() => StatusCode(StatusCodes.Status403Forbidden, new {
        ok = false,
        status = StatusCodes.Status403Forbidden,
        code = "ORGANIZATION_SCOPE_REQUIRED",
        message = "Selecione uma organização para acessar este recurso.",
        correlationId = HttpContext.TraceIdentifier
    });
}
