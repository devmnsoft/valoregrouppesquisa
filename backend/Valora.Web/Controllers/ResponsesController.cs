using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class ResponsesController(
    IResponseRepository responseRepo,
    ICurrentRequestContext currentRequest,
    ILogger<ResponsesController> logger) : Controller {

    private Guid? OrganizationId => currentRequest.GetCurrent().EffectiveOrganizationId;

    [HttpGet("/Responses")]
    public async Task<IActionResult> Index([FromQuery] string? search) {
        if (OrganizationId is not { } organizationId) return View("NoOrganization");

        try {
            var rawItems = await responseRepo.ListAdminAsync(organizationId);
            var list = new List<ResponseItemViewModel>(rawItems.Count);

            foreach (var item in rawItems) {
                var dict = (IDictionary<string, object?>)item;

                Guid id = dict.TryGetValue("id", out var idVal) && idVal is Guid gId ? gId : Guid.NewGuid();
                string surveyTitle = dict.TryGetValue("survey_title", out var sVal) && sVal is not null ? sVal.ToString()! : "Diagnóstico Geral";
                string formName = dict.TryGetValue("form_name", out var fVal) && fVal is not null ? fVal.ToString()! : "Formulário de Coleta";
                string partName = dict.TryGetValue("participant_name", out var pNameVal) && !string.IsNullOrWhiteSpace(pNameVal?.ToString())
                    ? pNameVal.ToString()! : "Participante Anônimo";
                string partEmail = dict.TryGetValue("participant_email", out var pEmailVal) && !string.IsNullOrWhiteSpace(pEmailVal?.ToString())
                    ? pEmailVal.ToString()! : "Não informado";
                string status = dict.TryGetValue("status", out var stVal) && stVal is not null ? stVal.ToString()! : "completed";

                DateTimeOffset createdAt = DateTimeOffset.UtcNow;
                if (dict.TryGetValue("created_at", out var dtVal) && dtVal is DateTime dt) {
                    createdAt = new DateTimeOffset(dt, TimeSpan.Zero);
                }

                DateTimeOffset? completedAt = null;
                if (dict.TryGetValue("completed_at", out var cdtVal) && cdtVal is DateTime cdt) {
                    completedAt = new DateTimeOffset(cdt, TimeSpan.Zero);
                }

                if (!string.IsNullOrWhiteSpace(search)) {
                    var s = search.Trim();
                    if (!surveyTitle.Contains(s, StringComparison.OrdinalIgnoreCase) &&
                        !formName.Contains(s, StringComparison.OrdinalIgnoreCase) &&
                        !partName.Contains(s, StringComparison.OrdinalIgnoreCase) &&
                        !partEmail.Contains(s, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }
                }

                list.Add(new ResponseItemViewModel(id, surveyTitle, formName, partName, partEmail, status, createdAt, completedAt));
            }

            return View(new ResponsesIndexViewModel(list, search));
        }
        catch (Exception ex) {
            logger.LogError(ex, "Erro ao listar respostas para a organização {OrganizationId}", organizationId);
            TempData["ErrorMessage"] = "Não foi possível carregar as respostas no momento.";
            return View(new ResponsesIndexViewModel([]));
        }
    }

    [HttpGet("/Responses/{id:guid}")]
    public async Task<IActionResult> Details(Guid id) {
        if (OrganizationId is not { } organizationId) return View("NoOrganization");

        try {
            var result = await responseRepo.GetAdminAsync(organizationId, id);
            if (result is null) {
                TempData["ErrorMessage"] = "Registro de resposta não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            return View("Details", new ResponseDetailViewModel(result));
        }
        catch (Exception ex) {
            logger.LogError(ex, "Erro ao obter detalhes da resposta {ResponseId}", id);
            TempData["ErrorMessage"] = "Não foi possível carregar os detalhes da resposta.";
            return RedirectToAction(nameof(Index));
        }
    }
}
