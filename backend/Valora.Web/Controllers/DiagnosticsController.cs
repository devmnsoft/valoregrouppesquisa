using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Valora.Web.Controllers;

[Authorize]
public sealed class DiagnosticsController : Controller {
    [HttpGet("Diagnostics")]
    public IActionResult Index() => RedirectToAction("Index", "Surveys");
    [HttpGet("Diagnostics/New")]
    public IActionResult New() => RedirectToAction("Index", "Forms");
    [HttpGet("Diagnostics/Create")]
    public IActionResult Create() => RedirectToAction(nameof(New));
    [HttpGet("Diagnostics/Audiences")]
    public IActionResult Audiences() => RedirectToAction("PublicLinks", "Surveys");
    [HttpGet("Diagnostics/{id:guid}")]
    public IActionResult Details(Guid id) => RedirectToAction(nameof(Workspace), new { id });
    [HttpGet("Diagnostics/{id:guid}/Workspace")]
    public IActionResult Workspace(Guid id) { ViewData["DiagnosticId"] = id; return View(); }
    [HttpGet("Diagnostics/{id:guid}/InDevelopment")]
    public IActionResult InDevelopment(Guid id) => View("InDevelopment", new Valora.Web.Models.ViewModels.InDevelopmentViewModel(
        "Módulo de Diagnóstico Avançado",
        "Configuração e enriquecimento avançado do ciclo diagnóstico.",
        "Permitirá configurar parâmetros estendidos de amostragem, recortes dinâmicos e integrações de escuta contínua.",
        "Gestores de Pesquisa, Consultores e Administradores da Organização.",
        "Parâmetros de amostragem, públicos-alvo, questionários revisados e datas de coleta.",
        "Ciclo de escuta preparado, com critérios de prontidão e links públicos seguros para coleta.",
        "Utilize a aba 'Preparação' no Workspace do Diagnóstico para configurar título, responsável, datas e metodologia.",
        $"/Diagnostics/{id}/Workspace?tab=preparation",
        $"/Diagnostics/{id}/Workspace"));
    [HttpGet("Diagnostics/Details/{id:guid}")]
    public IActionResult DetailsCanonical(Guid id) => RedirectToAction(nameof(Workspace), new { id });
    [HttpGet("Diagnostics/Collect/{id:guid}")]
    public IActionResult Collect(Guid id) => RedirectToAction("PublicLinks", "Surveys", new { surveyId = id });
    [HttpGet("Diagnostics/Results/{id:guid}")]
    public IActionResult Results(Guid id) => RedirectToAction("Details", "Results", new { id });
}
