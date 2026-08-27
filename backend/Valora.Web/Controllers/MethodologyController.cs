using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class MethodologyController : Controller
{
    [HttpGet("Methodology/Overview")]
    [HttpGet("Methodology")]
    [HttpGet("MethodologyStudio")]
    public IActionResult Overview() => View();

    [HttpGet("Methodology/Dimensions")]
    [HttpGet("MethodologyStudio/Dimensions")]
    public IActionResult Dimensions() => View(nameof(Overview));
    [HttpGet("Methodology/Concepts")]
    [HttpGet("MethodologyStudio/Concepts")]
    public IActionResult Concepts() => View(nameof(Overview));
    [HttpGet("Methodology/OfficialQuestions")]
    public IActionResult OfficialQuestions() => View(nameof(Overview));
    [HttpGet("Methodology/Templates")]
    [HttpGet("DiagnosticTemplates")]
    public IActionResult Templates() => View(nameof(Overview));
    [HttpGet("Methodology/Scoring")]
    [HttpGet("ScoringRules")]
    public IActionResult Scoring() => View(nameof(Overview));
    [HttpGet("Methodology/Recommendations")]
    public IActionResult Recommendations() => View(nameof(Overview));
    [HttpGet("Methodology/Versions")]
    public IActionResult Versions() => View(nameof(Overview));

    [HttpGet("Methodology/Dictionary")]
    [HttpGet("Intelligence/Dictionary")]
    public IActionResult Dictionary() => View();

    [HttpGet("Methodology/CognitiveMap")]
    [HttpGet("Intelligence/CognitiveMap")]
    public IActionResult CognitiveMap() => View();

    [HttpGet("Methodology/Mappings")]
    public IActionResult Mappings() => View();

    [HttpGet("Methodology/Map")]
    public IActionResult Map() => View(nameof(CognitiveMap));
    [HttpGet("Methodology/Indexes")]
    public IActionResult Indexes() => Studio("Índices Valora", "Escalas, faixas e estratégias oficiais de cálculo.");
    [HttpGet("Methodology/Questions")]
    [HttpGet("QuestionBank")]
    public IActionResult Questions() => Studio("Perguntas Oficiais", "Banco versionado, pesos e vínculos cognitivos.");
    [HttpGet("MethodologyStudio/MaturityLevels")]
    public IActionResult MaturityLevels() => Studio("Níveis de Maturidade", "Faixas e critérios verificáveis da evolução organizacional.");
    [HttpGet("MethodologyStudio/EvidenceCriteria")]
    public IActionResult EvidenceCriteria() => Studio("Critérios de Evidência", "Fontes, força e regras de uso das evidências metodológicas.");
    [HttpGet("Methodology/Prompts")]
    public IActionResult Prompts() => Studio("Prompts IA", "Templates oficiais, schemas de saída e versões.");
    [HttpGet("Methodology/Guardrails")]
    public IActionResult Guardrails() => Studio("Guardrails", "Princípios inegociáveis e proteção metodológica.");
    [HttpGet("Methodology/Validation")]
    public IActionResult Validation() => Studio("Validação", "Bloqueios, alertas e recomendações antes da publicação.");

    private IActionResult Studio(string title, string description)
    { ViewData["StudioTitle"] = title; ViewData["StudioDescription"] = description; return View("Studio"); }
}
