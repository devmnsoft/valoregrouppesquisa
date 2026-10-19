using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Processes;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class ProcessesController(
    ICurrentOrganizationProvider current,
    ProcessDefinitionService definitions,
    ProcessStepService steps,
    ProcessInstanceService instances,
    ProcessSlaService sla,
    ProcessBottleneckInsightService insights,
    ProcessTemplateService templates) : Controller {

    private readonly ICurrentOrganizationProvider _current = current;
    private readonly ProcessDefinitionService _definitions = definitions;
    private readonly ProcessStepService _steps = steps;
    private readonly ProcessInstanceService _instances = instances;
    private readonly ProcessSlaService _sla = sla;
    private readonly ProcessBottleneckInsightService _insights = insights;
    private readonly ProcessTemplateService _templates = templates;

    private Guid UserId => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) {
        var o = Scope();
        if (o is null) return View("NoOrganization");
        return View(new ProcessesIndexViewModel(await _instances.Dashboard(o.Value, ct), await _sla.List(o.Value, ct), await _insights.List(o.Value, ct)));
    }

    [HttpGet]
    public Task<IActionResult> Definitions(CancellationToken ct) => List("Definições", "Processos versionados, com dono e estrutura publicável.", ct, definitions: true);

    [HttpGet]
    public async Task<IActionResult> Builder(Guid? id, CancellationToken ct) {
        var o = Scope();
        if (o is null) return View("NoOrganization");

        var allDefinitions = await _definitions.List(o.Value, ct);
        ProcessDefinitionDto? selected = null;
        if (id.HasValue && id.Value != Guid.Empty) {
            selected = await _definitions.Get(o.Value, id.Value, ct);
        }

        selected ??= allDefinitions.FirstOrDefault(d => d.Status != "published") ?? allDefinitions.FirstOrDefault();

        IReadOnlyList<ProcessStepDto> stepList = selected is not null
            ? await _steps.List(o.Value, selected.Id, ct)
            : [];

        return View("Builder", new ProcessBuilderViewModel(selected, stepList, allDefinitions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProcess([FromForm] CreateProcessRequest request, CancellationToken ct) {
        var o = Scope();
        if (o is null) return View("NoOrganization");

        var ownerId = request.OwnerUserId != Guid.Empty ? request.OwnerUserId : UserId;
        if (ownerId == Guid.Empty) {
            TempData["ProcessError"] = "Identificador de responsável pelo processo é obrigatório.";
            return RedirectToAction(nameof(Builder));
        }

        try {
            var sanitizedReq = new CreateProcessRequest {
                Name = request.Name?.Trim() ?? "",
                Description = request.Description?.Trim() ?? "",
                Category = request.Category?.Trim() ?? "Geral",
                OwnerUserId = ownerId
            };
            var newId = await _definitions.Create(o.Value, UserId, sanitizedReq, ct);
            TempData["ProcessSuccess"] = "Processo criado como rascunho com sucesso. Adicione as etapas.";
            return RedirectToAction(nameof(Builder), new { id = newId });
        }
        catch (Exception ex) {
            TempData["ProcessError"] = ex.Message;
            return RedirectToAction(nameof(Builder));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStep(Guid processId, [FromForm] CreateProcessStepRequest request, CancellationToken ct) {
        var o = Scope();
        if (o is null) return View("NoOrganization");

        try {
            await _steps.Create(o.Value, processId, request, ct);
            TempData["ProcessSuccess"] = "Etapa adicionada com sucesso ao fluxo.";
        }
        catch (Exception ex) {
            TempData["ProcessError"] = ex.Message;
        }

        return RedirectToAction(nameof(Builder), new { id = processId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct) {
        var o = Scope();
        if (o is null) return View("NoOrganization");

        try {
            await _definitions.Publish(o.Value, id, UserId, ct);
            TempData["ProcessSuccess"] = "Processo validado e publicado com sucesso!";
        }
        catch (Exception ex) {
            TempData["ProcessError"] = ex.Message;
        }

        return RedirectToAction(nameof(Builder), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewVersion(Guid id, CancellationToken ct) {
        var o = Scope();
        if (o is null) return View("NoOrganization");

        try {
            var newId = await _definitions.NewVersion(o.Value, id, UserId, ct);
            TempData["ProcessSuccess"] = "Nova versão gerada em rascunho para edição.";
            return RedirectToAction(nameof(Builder), new { id = newId });
        }
        catch (Exception ex) {
            TempData["ProcessError"] = ex.Message;
            return RedirectToAction(nameof(Builder), new { id });
        }
    }

    [HttpGet]
    public Task<IActionResult> Instances(CancellationToken ct) => List("Instâncias", "Execuções reais, responsáveis, origens e prazos.", ct, instances: true);

    [HttpGet]
    public Task<IActionResult> Approvals(CancellationToken ct) => List("Aprovações", "Decisões humanas preservadas e integralmente rastreáveis.", ct, instances: true);

    [HttpGet]
    public Task<IActionResult> Sla(CancellationToken ct) => List("Painel de SLA", "Prazos em dia, em risco e atrasados, sem inferir dados ausentes.", ct, sla: true);

    [HttpGet]
    public Task<IActionResult> Insights(CancellationToken ct) => List("Gargalos", "Leituras baseadas exclusivamente no histórico das execuções.", ct, insights: true);

    [HttpGet]
    public Task<IActionResult> Templates(CancellationToken ct) => List("Templates de processos", "Modelos adaptáveis, sujeitos à validação e publicação humana.", ct, templates: true);

    private async Task<IActionResult> List(string title, string description, CancellationToken ct, bool definitions = false, bool instances = false, bool sla = false, bool insights = false, bool templates = false) {
        var o = Scope();
        if (o is null) return View("NoOrganization");
        return View("List", new ProcessListViewModel(
            title,
            description,
            definitions ? await _definitions.List(o.Value, ct) : [],
            instances ? await _instances.List(o.Value, ct) : [],
            sla ? await _sla.List(o.Value, ct) : [],
            insights ? await _insights.List(o.Value, ct) : [],
            templates ? await _templates.List(o.Value, ct) : []));
    }

    private Guid? Scope() {
        var scope = _current.GetCurrent();
        if (scope.IsResolved && scope.OrganizationId != Guid.Empty) return scope.OrganizationId;
        TempData["ProcessError"] = "Selecione uma organização para acessar os processos.";
        return null;
    }
}
