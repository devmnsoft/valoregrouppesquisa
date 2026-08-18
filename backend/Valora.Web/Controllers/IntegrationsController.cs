using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class IntegrationsController : Controller
{
    [HttpGet("Integrations"), HttpGet("Administration/Integrations")]
    public IActionResult Index() => View();

    [HttpGet("Integrations/ApiKeys"), HttpGet("Administration/ApiKeys")]
    public IActionResult ApiKeys() => View("Index", "api-keys");

    [HttpGet("Integrations/Webhooks")]
    public IActionResult Webhooks() => View("Index", "webhooks");

    [HttpGet("Integrations/PowerBI"), HttpGet("Intelligence/PowerBI")]
    public IActionResult PowerBi() => View("Index", "powerbi");

    [HttpGet("Integrations/OneOnOne")]
    public IActionResult OneOnOne() => RedirectToAction("OneOnOne", "Intelligence");

    [HttpGet("Integrations/Imports")]
    public IActionResult Imports() => RedirectToAction("Index", "Enterprise", new { module = "imports" });
}
