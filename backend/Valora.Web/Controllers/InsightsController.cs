using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.ValoraAi;
using Valora.Application.Access;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize(Policy = ValoraPermissions.Insights.Read)]
[Route("Insights")]
public sealed class InsightsController(
    IValoraAiInsightRepository insights,
    ApproveAiInsightUseCase approveInsight,
    RejectAiInsightUseCase rejectInsight,
    ILogger<InsightsController> logger) : Controller {
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct) {
        if (!TryGetOrganizationId(out var organizationId)) return OrganizationRequired();
        return View(await insights.ListAsync(organizationId, null, ct));
    }

    [HttpGet("Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct) {
        if (!TryGetOrganizationId(out var organizationId)) return OrganizationRequired();
        var insight = await insights.GetAsync(organizationId, id, ct);
        return insight is null ? NotFound() : View(insight);
    }

    [HttpPost("Details/{id:guid}/Approve")]
    [Authorize(Policy = ValoraPermissions.Insights.Approve)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, string commandKey, long expectedVersion, CancellationToken ct) {
        if (!TryGetOperationContext(out var organizationId, out var userId)) return OrganizationRequired();

        try {
            await approveInsight.ExecuteAsync(organizationId, id, userId, commandKey, expectedVersion, ct);
            TempData["Success"] = "Insight aprovado com sucesso. A revisão humana foi registrada.";
        }
        catch (Valora.Application.Exceptions.ConcurrencyConflictException exception) {
            logger.LogWarning(exception, "Conflito ao aprovar insight {InsightId}. CorrelationId={CorrelationId}", id, HttpContext.TraceIdentifier);
            TempData["Warning"] = "Outra pessoa alterou este insight. Atualize a página antes de decidir novamente.";
        }
        catch (Exception exception) when (exception is KeyNotFoundException or InvalidOperationException or ArgumentException) {
            logger.LogWarning(exception,
                "Não foi possível aprovar o insight. InsightId={InsightId} OrganizationId={OrganizationId} UserId={UserId} CorrelationId={CorrelationId}",
                id, organizationId, userId, HttpContext.TraceIdentifier);
            TempData["Warning"] = "Este insight não está disponível para aprovação. Atualize a página e revise o status.";
        }
        catch (Exception exception) {
            logger.LogError(exception,
                "Falha ao aprovar insight. InsightId={InsightId} OrganizationId={OrganizationId} UserId={UserId} CorrelationId={CorrelationId}",
                id, organizationId, userId, HttpContext.TraceIdentifier);
            TempData["Error"] = "Não foi possível concluir a operação. Tente novamente.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("Details/{id:guid}/Reject")]
    [Authorize(Policy = ValoraPermissions.Insights.Reject)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, RejectAiInsightViewModel model, CancellationToken ct) {
        if (!TryGetOperationContext(out var organizationId, out var userId)) return OrganizationRequired();
        model.InsightId = id;
        model.Reason = model.Reason?.Trim() ?? string.Empty;
        if (!ModelState.IsValid) {
            var insight = await insights.GetAsync(organizationId, id, ct);
            if (insight is null) return NotFound();
            ViewData["OpenRejectDialog"] = true;
            ViewData["RejectionReason"] = model.Reason;
            return View("Details", insight);
        }

        try {
            await rejectInsight.ExecuteAsync(organizationId, id, userId, model.Reason, model.CommandKey, model.ExpectedVersion, ct);
            TempData["Success"] = "Insight rejeitado. O motivo e a revisão humana foram registrados.";
        }
        catch (Valora.Application.Exceptions.ConcurrencyConflictException exception) {
            logger.LogWarning(exception, "Conflito ao rejeitar insight {InsightId}. CorrelationId={CorrelationId}", id, HttpContext.TraceIdentifier);
            TempData["OpenRejectDialog"] = true;
            TempData["Warning"] = "Outra pessoa alterou este insight. Atualize os dados; sua justificativa foi preservada.";
        }
        catch (Exception exception) when (exception is KeyNotFoundException or InvalidOperationException or ArgumentException) {
            logger.LogWarning(exception,
                "Não foi possível rejeitar o insight. InsightId={InsightId} OrganizationId={OrganizationId} UserId={UserId} CorrelationId={CorrelationId}",
                id, organizationId, userId, HttpContext.TraceIdentifier);
            TempData["Warning"] = "Este insight não está disponível para rejeição. Atualize a página e revise o status.";
        }
        catch (Exception exception) {
            logger.LogError(exception,
                "Falha ao rejeitar insight. InsightId={InsightId} OrganizationId={OrganizationId} UserId={UserId} CorrelationId={CorrelationId}",
                id, organizationId, userId, HttpContext.TraceIdentifier);
            TempData["Error"] = "Não foi possível concluir a operação. Tente novamente.";
            TempData["OpenRejectDialog"] = true;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private bool TryGetOrganizationId(out Guid organizationId) =>
        Guid.TryParse(User.FindFirstValue("organization_id") ?? User.FindFirstValue("organizationId"), out organizationId)
        && organizationId != Guid.Empty;

    private bool TryGetOperationContext(out Guid organizationId, out Guid userId) {
        userId = Guid.Empty;
        return TryGetOrganizationId(out organizationId)
            && Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId)
            && userId != Guid.Empty;
    }

    private IActionResult OrganizationRequired() {
        TempData["Warning"] = "Selecione uma organização para continuar.";
        return RedirectToAction("Index", "Organization");
    }
}
