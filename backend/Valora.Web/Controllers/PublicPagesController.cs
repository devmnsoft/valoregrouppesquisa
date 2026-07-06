using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class PublicPagesController(ILogger<PublicPagesController> logger) : Controller
{
    [Route("diagnostico-gratuito")]
    public IActionResult FreeDiagnostic() => PublicView("FreeDiagnostic", "Diagnóstico gratuito");

    [Route("contato")]
    public IActionResult Contact() => PublicView("Contact", "Contato");

    [Route("whatsapp")]
    public IActionResult WhatsApp() => PublicView("WhatsApp", "WhatsApp");

    [Route("entrar")]
    public IActionResult LoginRedirect() => Redirect("/Account/Login");

    private IActionResult PublicView(string viewName, string title)
    {
        try { ViewData["Title"] = title; return View(viewName); }
        catch (Exception ex) { logger.LogError(ex, "Falha ao renderizar página pública {ViewName}.", viewName); throw; }
    }
}
