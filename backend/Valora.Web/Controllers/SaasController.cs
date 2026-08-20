using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class SaasController : Controller
{
    [HttpGet("/Platform/Plans")] public IActionResult Plans() => Page("Planos", "Catálogo comercial", "plans");
    [HttpGet("/Platform/Subscriptions")] public IActionResult Subscriptions() => Page("Assinaturas", "Governança por organização", "subscriptions");
    [HttpGet("/Platform/Invoices")] public IActionResult Invoices() => Page("Faturas", "Cobrança e financeiro", "invoices");
    [HttpGet("/Platform/Usage")] public IActionResult Usage() => Page("Uso", "Consumo contratado", "usage");
    [HttpGet("/Organization/MyPlan")] public IActionResult MyPlan() => Page("Meu Plano", "Assinatura, recursos e limites", "my-plan");
    [HttpGet("/Organization/Consumption")] public IActionResult Consumption() => Page("Consumo", "Uso mensal da organização", "usage");
    [HttpGet("/Organization/Upgrade")] public IActionResult Upgrade() => Page("Upgrade", "Evolua sem perder seus dados", "upgrade");

    private IActionResult Page(string title, string subtitle, string mode)
    {
        ViewData["Title"] = title;
        ViewData["Subtitle"] = subtitle;
        ViewData["Mode"] = mode;
        return View("Dashboard");
    }
}
