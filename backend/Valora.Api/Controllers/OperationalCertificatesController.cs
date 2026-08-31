using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Api.Controllers;

[Authorize, ApiController]
public sealed class OperationalCertificatesController(ICertificateOperationalService certs, IEmailQueueService email) : ControllerBase
{
    private Guid? OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) && id != Guid.Empty ? id : null;

    [HttpPost("/certificates/responses/{responseId:guid}/generate")]
    public async Task<IActionResult> Generate(Guid responseId)
    {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        try
        {
            return Ok(new { ok = true, certificate = await certs.GenerateAsync(organizationId, responseId) });
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { ok = false, code = exception.Message });
        }
    }

    [HttpGet("/certificates")]
    public async Task<IActionResult> List()
    {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        return Ok(new { ok = true, data = await certs.ListAsync(organizationId) });
    }

    [HttpGet("/certificates/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        var html = await certs.DownloadHtmlAsync(organizationId, id);
        return html is null ? NotFound() : Content(html, "text/html");
    }

    [HttpPatch("/certificates/{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        await certs.RevokeAsync(organizationId, id);
        return Ok(new { ok = true });
    }

    [HttpPost("/certificates/{certificateId:guid}/send-email")]
    public async Task<IActionResult> Send(Guid certificateId, [FromBody] SendCertificateEmailRequest request)
    {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        return Ok(new { ok = true, job = await email.QueueCertificateAsync(organizationId, certificateId, request.ToEmail) });
    }

    private ObjectResult OrganizationRequired() => StatusCode(StatusCodes.Status403Forbidden, new
    {
        ok = false,
        code = "ORGANIZATION_REQUIRED",
        message = "Selecione uma organização para continuar."
    });
}
