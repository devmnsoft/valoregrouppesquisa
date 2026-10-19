using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class ExportsController(
    IExportService exports,
    ICurrentRequestContext currentRequest,
    ILogger<ExportsController> logger) : Controller {

    private Guid? OrganizationId => currentRequest.GetCurrent().EffectiveOrganizationId;
    private Guid? UserId => currentRequest.GetCurrent().UserId;

    [HttpGet("/Exports")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) {
        if (OrganizationId is not { } organizationId) return View("NoOrganization");

        try {
            var jobs = await exports.ListAsync(organizationId, cancellationToken);
            return View(new ExportsIndexViewModel(jobs));
        }
        catch (Exception ex) {
            logger.LogError(ex, "Erro ao carregar central de exportações para a organização {OrganizationId}", organizationId);
            TempData["ErrorMessage"] = "Não foi possível carregar a lista de exportações no momento.";
            return View(new ExportsIndexViewModel([]));
        }
    }

    [HttpPost("/Exports/Request")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestExport([FromForm] string entity, [FromForm] string format, [FromForm] string? filterJson, CancellationToken cancellationToken) {
        if (OrganizationId is not { } organizationId) return View("NoOrganization");

        try {
            var req = new ExportRequest(entity?.Trim().ToLowerInvariant() ?? "", format?.Trim().ToLowerInvariant() ?? "csv", filterJson);
            await exports.RequestAsync(organizationId, UserId, req, HttpContext.TraceIdentifier, cancellationToken);
            TempData["SuccessMessage"] = "Solicitação de exportação registrada com sucesso. O arquivo está sendo processado.";
        }
        catch (InvalidOperationException ex) {
            logger.LogWarning(ex, "Regra violada ao solicitar exportação da entidade {Entity}", entity);
            TempData["ErrorMessage"] = ex.Message switch {
                "MODULE_NOT_ENABLED" => "O módulo de exportações não está habilitado para o seu plano.",
                "INVALID_EXPORT_ENTITY" => "Entidade de exportação inválida ou não autorizada.",
                "INVALID_EXPORT_FORMAT" => "Formato de exportação não suportado para esta entidade.",
                _ => "Não foi possível solicitar a exportação: " + ex.Message
            };
        }
        catch (Exception ex) {
            logger.LogError(ex, "Erro inesperado ao solicitar exportação");
            TempData["ErrorMessage"] = "Ocorreu um erro ao processar sua solicitação de exportação.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/Exports/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken) {
        if (OrganizationId is not { } organizationId) return View("NoOrganization");

        try {
            var job = await exports.GetAsync(organizationId, id, cancellationToken);
            if (job is null || job.ResultPayload is null || job.Status != "completed" || job.ExpiresAt <= DateTimeOffset.UtcNow) {
                TempData["ErrorMessage"] = "O arquivo solicitado não está disponível, expirou ou ainda está em processamento.";
                return RedirectToAction(nameof(Index));
            }

            var bytes = Convert.FromBase64String(job.ResultPayload);
            var mime = string.IsNullOrWhiteSpace(job.ResultMimeType) ? "application/octet-stream" : job.ResultMimeType;
            var fileName = string.IsNullOrWhiteSpace(job.ResultFileName) ? $"export_{job.Entity}_{job.Id}.{job.Format}" : job.ResultFileName;

            return File(bytes, mime, fileName);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Falha ao obter download de exportação {ExportId}", id);
            TempData["ErrorMessage"] = "Erro ao baixar o arquivo exportado.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet("/Exports/{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, CancellationToken cancellationToken) {
        if (OrganizationId is not { } organizationId) return Unauthorized(new { ok = false, message = "Sessão inválida" });

        var job = await exports.GetAsync(organizationId, id, cancellationToken);
        if (job is null) return NotFound(new { ok = false, message = "Exportação não encontrada" });

        return Ok(new {
            ok = true,
            id = job.Id,
            status = job.Status,
            errorMessage = job.ErrorMessage,
            expiresAt = job.ExpiresAt,
            isCompleted = job.Status == "completed",
            isFailed = job.Status == "failed",
            downloadUrl = job.Status == "completed" ? $"/Exports/{job.Id}/download" : null
        });
    }
}
