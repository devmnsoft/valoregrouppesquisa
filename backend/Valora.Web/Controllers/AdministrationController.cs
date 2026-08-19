using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class AdministrationController : Controller
{
    [HttpGet("Administration/Privacy")]
    [HttpGet("Privacy")]
    public IActionResult Privacy() => Module("LGPD e Privacidade", "Solicitações, protocolos e tratamento administrativo de dados.", "/bff/privacy/requests", "privacy");

    [HttpGet("Notifications")]
    public IActionResult Notifications() => Module("Notificações", "Central administrativa da sua organização.", "/bff/notifications", "notifications");

    [HttpGet("Administration/Governance")]
    [HttpGet("PlatformGovernance")]
    public IActionResult Governance() => Module("Governança da Plataforma", "Rastreabilidade de alterações, acessos e decisões do SaaS.", "/bff/platform-governance", "governance");

    [HttpGet("SystemHealth")]
    public IActionResult SystemHealth() => Module("Saúde do Sistema", "Checks administrativos sem exposição de segredos.", "/bff/system-health", "health");

    private IActionResult Module(string title, string subtitle, string endpoint, string module)
    {
        ViewData["Title"] = title;
        ViewData["Subtitle"] = subtitle;
        ViewData["Endpoint"] = endpoint;
        ViewData["Module"] = module;
        return View("Module");
    }
}
