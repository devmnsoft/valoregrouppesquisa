using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Subscriptions;

namespace Valora.Api.Controllers;

[Authorize]
[ApiController]
public sealed class ReportsController(
    IReportService reports,
    CheckFeatureAccessUseCase featureAccess,
    ValidateUsageLimitUseCase limits,
    RegisterUsageEventUseCase usage) : ControllerBase
{
    private Guid OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) ? id : Guid.Empty;
    private Guid? UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet("/reports/surveys/{surveyId:guid}")]
    public Task<IActionResult> Survey(Guid surveyId, CancellationToken ct) => GenerateAsync(
        () => reports.GenerateSurveyAsync(OrganizationId, surveyId, "html", UserId), false, ct);

    [HttpGet("/reports/responses/{responseId:guid}")]
    public Task<IActionResult> Response(Guid responseId, CancellationToken ct) => GenerateAsync(
        () => reports.GenerateResponseAsync(OrganizationId, responseId, "html", UserId), false, ct);

    [HttpGet("/reports/organization")]
    public Task<IActionResult> Organization(CancellationToken ct) => GenerateAsync(
        () => reports.GenerateOrganizationAsync(OrganizationId, "html", UserId), true, ct);

    [HttpPost("/reports/organization/generate")]
    public Task<IActionResult> GenerateOrganization([FromBody] GenerateReportRequest request, CancellationToken ct) => GenerateAsync(
        () => reports.GenerateOrganizationAsync(OrganizationId, request.Format, UserId), true, ct);

    [HttpPost("/reports/surveys/{surveyId:guid}/generate")]
    public Task<IActionResult> Generate(Guid surveyId, [FromBody] GenerateReportRequest request, CancellationToken ct) => GenerateAsync(
        () => reports.GenerateSurveyAsync(OrganizationId, surveyId, request.Format, UserId), false, ct);

    [HttpGet("/reports/generated")]
    public async Task<IActionResult> Generated() => Ok(new { ok = true, data = await reports.ListGeneratedAsync(OrganizationId) });

    [HttpGet("/reports/generated/{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var report = await reports.GetGeneratedAsync(OrganizationId, id);
        return report is null ? NotFound(new { ok = false, message = "Relatório não encontrado." }) : Ok(new { ok = true, report });
    }

    private async Task<IActionResult> GenerateAsync<T>(Func<Task<T>> generate, bool executive, CancellationToken ct)
    {
        if (OrganizationId == Guid.Empty) return BadRequest(BusinessError("ORGANIZATION_REQUIRED", "Selecione uma organização para consultar os limites do plano."));
        var feature = await featureAccess.ExecuteAsync(OrganizationId,
            executive ? SubscriptionFeatures.ExecutiveReports : SubscriptionFeatures.Reports, ct);
        if (!feature.Allowed) return StatusCode(StatusCodes.Status403Forbidden, BusinessError("FEATURE_BLOCKED", feature.Message, feature.UpgradeUrl));
        var limit = await limits.ExecuteAsync(OrganizationId, SubscriptionMetrics.Reports, 1, ct);
        if (!limit.Allowed) return Conflict(BusinessError("PLAN_LIMIT_REACHED", limit.Message, limit.UpgradeUrl));
        try
        {
            var report = await generate();
            await usage.ExecuteAsync(OrganizationId, SubscriptionMetrics.Reports, 1, cancellationToken: ct);
            return Ok(new { ok = true, report });
        }
        catch (InvalidOperationException)
        {
            return UnprocessableEntity(BusinessError("REPORT_NOT_AVAILABLE", "Não foi possível gerar o relatório com os dados atuais."));
        }
    }

    private object BusinessError(string code, string message, string? upgradeUrl = null) =>
        new { ok = false, code, message, upgradeUrl, correlationId = HttpContext.TraceIdentifier };
}
