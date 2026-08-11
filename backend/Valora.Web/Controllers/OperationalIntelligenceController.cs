using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class OperationalIntelligenceController : Controller
{
    [HttpGet("Comparativos")]
    [HttpGet("OperationalIntelligence/Comparisons")]
    public IActionResult Comparisons() => View();

    [HttpGet("Recomendacoes")]
    [HttpGet("OperationalIntelligence/Recommendations")]
    public IActionResult Recommendations() => View();

    [HttpGet("PlanoDeAcao")]
    [HttpGet("OperationalIntelligence/ActionPlans")]
    public IActionResult ActionPlans() => View();
}
