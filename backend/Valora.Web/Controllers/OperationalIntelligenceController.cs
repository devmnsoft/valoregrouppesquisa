using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class OperationalIntelligenceController : Controller
{
    [HttpGet("Comparativos")]
    public IActionResult Comparisons() => View();

    [HttpGet("Recomendacoes")]
    public IActionResult Recommendations() => View();

    [HttpGet("PlanoDeAcao")]
    public IActionResult ActionPlans() => View();
}
