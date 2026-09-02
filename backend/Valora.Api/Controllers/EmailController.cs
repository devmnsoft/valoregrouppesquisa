using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Api.Controllers;

[Authorize]
[ApiController]
public sealed class EmailController(IEmailTemplateService templates, IEmailQueueService queue, IEmailSenderService sender, IEmailStatusService status) : ControllerBase
{
    private Guid? OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) && id != Guid.Empty ? id : null;

    [HttpGet("/email/templates")]
    public async Task<IActionResult> Templates() => OrganizationId is Guid organizationId
        ? Ok(new { ok = true, data = await templates.ListAsync(organizationId) })
        : OrganizationRequired();

    [HttpPost("/email/templates")]
    public async Task<IActionResult> Create([FromBody] UpsertEmailTemplateRequest request) => OrganizationId is Guid organizationId
        ? Ok(new { ok = true, template = await templates.UpsertAsync(null, ForCurrentOrganization(request, organizationId)) })
        : OrganizationRequired();

    [HttpPut("/email/templates/{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] UpsertEmailTemplateRequest request) => OrganizationId is Guid organizationId
        ? Ok(new { ok = true, template = await templates.UpsertAsync(id, ForCurrentOrganization(request, organizationId)) })
        : OrganizationRequired();

    [HttpGet("/email/jobs")]
    public IActionResult Jobs() => Ok(new { ok = true, status = "safe_metadata_only" });

    [HttpPost("/email/jobs/{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id) => Ok(new { ok = true, job = await queue.RetryAsync(id) });

    [HttpPost("/email/jobs/process")]
    public async Task<IActionResult> Process() => Ok(new { ok = true, processed = await sender.ProcessAsync() });

    [HttpGet("/email/status")]
    public async Task<IActionResult> Status() => OrganizationId is Guid organizationId
        ? Ok(new { ok = true, status = await status.GetAsync(organizationId) })
        : OrganizationRequired();

    [HttpPost("/responses/{responseId:guid}/send-result-email")]
    public async Task<IActionResult> Result(Guid responseId, [FromBody] Dictionary<string, string> body) => OrganizationId is Guid organizationId
        ? Ok(new { ok = true, job = await queue.QueueResultAsync(organizationId, responseId, body.GetValueOrDefault("toEmail") ?? string.Empty) })
        : OrganizationRequired();

    [HttpPost("/surveys/{surveyId:guid}/send-invites")]
    public async Task<IActionResult> Invite(Guid surveyId, [FromBody] Dictionary<string, string> body) => OrganizationId is Guid organizationId
        ? Ok(new { ok = true, job = await queue.QueueInviteAsync(organizationId, surveyId, body.GetValueOrDefault("toEmail") ?? string.Empty) })
        : OrganizationRequired();

    private static UpsertEmailTemplateRequest ForCurrentOrganization(UpsertEmailTemplateRequest request, Guid organizationId) =>
        request with { OrganizationId = organizationId };

    private ObjectResult OrganizationRequired() => Problem(statusCode: StatusCodes.Status403Forbidden,
        title: "Organização necessária", detail: "Selecione uma organização antes de gerenciar comunicações.",
        type: "https://valora.insight/problems/organization-required");
}
