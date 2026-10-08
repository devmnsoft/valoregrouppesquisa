using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class OrganizationalCentersController : Controller {
    // Bloco B — alias GET seguro: a rota canônica é /Workspace/Priorities.
    [HttpGet("Priorities")] public IActionResult Priorities() => Redirect("/Workspace/Priorities");
    // Bloco B — alias GET seguro: a rota canônica é /Intelligence/Evidence.
    [HttpGet("Evidence")] public IActionResult Evidence() => Redirect("/Intelligence/Evidence");
    [HttpGet("Evidence/Details/{id:guid}")] public IActionResult EvidenceDetails(Guid id) => Center("Detalhe da evidência", "Rastreabilidade da evidência", $"Referência: {id}. Confira contexto, classificação e usos autorizados.", "Esta evidência ainda não possui vínculos publicados.", "/Evidence");
    [HttpGet("Evidence/ByDiagnostic/{id:guid}")] public IActionResult EvidenceByDiagnostic(Guid id) => Center("Evidências do diagnóstico", "Leitura consolidada por ciclo", $"Diagnóstico: {id}. Evidências quantitativas e qualitativas permanecem separadas e rastreáveis.", "A coleta ainda não produziu evidências processadas.", "/Diagnostics");
    // Bloco B — alias GET seguro: a rota canônica é /Intelligence/Indices.
    [HttpGet("Indexes")] public IActionResult Indexes() => Redirect("/Intelligence/Indices");
    [HttpGet("Indexes/Details/{code}")] public IActionResult IndexDetails(string code) => Center(code.ToUpperInvariant(), "Composição do índice", "Veja fatores, evidências, riscos, oportunidades, recomendações e histórico deste índice.", "Ainda não há ciclos suficientes para apresentar a evolução deste índice.", "/Indexes");
    // Bloco B — alias GET seguro: a rota canônica é /Intelligence/Radar.
    [HttpGet("Radar")] public IActionResult Radar() => Redirect("/Intelligence/Radar");

    private IActionResult Center(string title, string eyebrow, string description, string empty, string cta) {
        ViewData["Title"] = title;
        return View("~/Views/OrganizationalCenters/Center.cshtml", new OrganizationalCenterViewModel(title, eyebrow, description, empty, cta));
    }
}

public sealed record OrganizationalCenterViewModel(string Title, string Eyebrow, string Description, string EmptyMessage, string CtaUrl);
