using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Indicators;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize, Route("CommandCenter")]
public sealed class CommandCenterController(
    IndicatorService indicators,
    IndicatorTargetService targets,
    IndicatorMeasurementService measurements,
    IndicatorAlertService alerts,
    ILogger<CommandCenterController> logger) : Controller
{
    private Guid OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id") ?? User.FindFirstValue("organizationId"), out var id) ? id : Guid.Empty;
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    [HttpGet("")] public Task<IActionResult> Index([FromQuery] CommandCenterFilterViewModel filter, CancellationToken ct) => Show("Overview", filter, ct);
    [HttpGet("Executive")] public Task<IActionResult> Executive([FromQuery] CommandCenterFilterViewModel filter, CancellationToken ct) => Show("Executive", filter, ct);
    [HttpGet("Metrics")] public Task<IActionResult> Metrics([FromQuery] CommandCenterFilterViewModel filter, CancellationToken ct) => Show("Metrics", filter, ct);
    [HttpGet("Alerts")] public Task<IActionResult> Alerts([FromQuery] CommandCenterFilterViewModel filter, CancellationToken ct) => Show("Alerts", filter, ct);
    [HttpGet("Timeline")] public Task<IActionResult> Timeline([FromQuery] CommandCenterFilterViewModel filter, CancellationToken ct) => Show("Timeline", filter, ct);
    [HttpGet("Actions")] public Task<IActionResult> Actions([FromQuery] CommandCenterFilterViewModel filter, CancellationToken ct) => Show("Actions", filter, ct);

    [ValidateAntiForgeryToken, HttpPost("Alerts/Resolve")]
    public async Task<IActionResult> ResolveAlert(ResolveCommandCenterAlertViewModel request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return await Show("Alerts", new() { Severity = request.ReturnSeverity }, ct);
        if (OrganizationId == Guid.Empty || UserId == Guid.Empty) return Forbid();
        try
        {
            await alerts.Resolve(OrganizationId, request.AlertId, UserId, ct);
            logger.LogInformation("CommandCenter alert resolved. AlertId={AlertId} OrganizationId={OrganizationId} UserId={UserId} CorrelationId={CorrelationId}", request.AlertId, OrganizationId, UserId, HttpContext.TraceIdentifier);
            TempData["Success"] = "Alerta marcado como resolvido.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CommandCenter alert resolution failed. AlertId={AlertId} OrganizationId={OrganizationId} CorrelationId={CorrelationId}", request.AlertId, OrganizationId, HttpContext.TraceIdentifier);
            TempData["Error"] = "Não foi possível resolver o alerta agora.";
        }
        return RedirectToAction(nameof(Alerts), new { severity = request.ReturnSeverity });
    }

    private async Task<IActionResult> Show(string page, CommandCenterFilterViewModel filter, CancellationToken ct)
    {
        ViewData["Title"] = "Valora Command Center™";
        if (OrganizationId == Guid.Empty) return View("MissingOrganization");
        try
        {
            var metricList = await indicators.List(OrganizationId, ct);
            var trendMap = new Dictionary<Guid, TrendResult>();
            foreach (var metric in metricList) trendMap[metric.Id] = await measurements.Trend(OrganizationId, metric.Id, ct);
            var alertList = await alerts.List(OrganizationId, ct);
            if (!string.IsNullOrWhiteSpace(filter.Severity)) alertList = alertList.Where(x => x.Severity.Equals(filter.Severity, StringComparison.OrdinalIgnoreCase)).ToArray();
            return View("Index", new CommandCenterViewModel(page, filter, metricList, await targets.List(OrganizationId, null, ct), alertList, trendMap));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CommandCenter load failed. Page={Page} OrganizationId={OrganizationId} CorrelationId={CorrelationId}", page, OrganizationId, HttpContext.TraceIdentifier);
            TempData["Error"] = "Não foi possível carregar o painel agora.";
            return View("Index", new CommandCenterViewModel(page, filter, [], [], [], new Dictionary<Guid, TrendResult>()));
        }
    }
}
