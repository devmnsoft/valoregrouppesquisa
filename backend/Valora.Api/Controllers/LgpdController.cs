using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class LgpdController(ILgpdConsentService consents, IPrivacyRequestService privacy) : ControllerBase
{
    private Guid? OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) && id != Guid.Empty ? id : null;
    private Guid? UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id != Guid.Empty ? id : null;

    [HttpPost("/public/lgpd/requests")]
    public async Task<IActionResult> PublicCreate([FromBody] CreatePrivacyRequestRequest request) =>
        Ok(new { ok = true, request = await privacy.CreatePublicAsync(request) });

    [HttpGet("/public/lgpd/requests/{protocol}")]
    public async Task<IActionResult> PublicGet(string protocol)
    {
        var request = await privacy.GetPublicAsync(protocol);
        return request is null ? NotFound() : Ok(new { ok = true, request });
    }

    [Authorize, HttpGet("/lgpd/consents")]
    public async Task<IActionResult> Consents()
    {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        return Ok(new { ok = true, data = await consents.ListAsync(organizationId) });
    }

    [Authorize, HttpGet("/lgpd/privacy-requests")]
    public async Task<IActionResult> Requests()
    {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        return Ok(new { ok = true, data = await privacy.ListAsync(organizationId) });
    }

    [Authorize, HttpGet("/lgpd/privacy-requests/{id:guid}")]
    public async Task<IActionResult> Request(Guid id)
    {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        var request = (await privacy.ListAsync(organizationId)).FirstOrDefault(item => item.Id == id);
        return request is null ? NotFound() : Ok(new { ok = true, request });
    }

    [Authorize, HttpPatch("/lgpd/privacy-requests/{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, [FromBody] UpdatePrivacyRequestStatusRequest request)
    {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        await privacy.UpdateStatusAsync(organizationId, id, request.Status, UserId);
        return Ok(new { ok = true });
    }

    [Authorize, HttpPost("/lgpd/privacy-requests/{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompletePrivacyRequestRequest request)
    {
        if (OrganizationId is not { } organizationId) return OrganizationRequired();
        await privacy.CompleteAsync(organizationId, id, request.ResultJson, UserId);
        return Ok(new { ok = true });
    }

    private ObjectResult OrganizationRequired() => StatusCode(StatusCodes.Status403Forbidden, new
    {
        ok = false,
        code = "ORGANIZATION_REQUIRED",
        message = "Selecione uma organização para continuar."
    });
}
