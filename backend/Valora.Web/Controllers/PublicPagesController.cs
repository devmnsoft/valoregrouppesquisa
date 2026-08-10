using Microsoft.AspNetCore.Mvc;
using Valora.Web.Models;

namespace Valora.Web.Controllers;

public sealed class PublicPagesController(ILogger<PublicPagesController> logger) : Controller
{
    [Route("diagnostico-gratuito")]
    public IActionResult FreeDiagnostic() => PublicView("FreeDiagnostic", "Diagnóstico gratuito");

    [Route("PublicPages/Contact")]
    [Route("contato")]
    public IActionResult Contact() => PublicView("Contact", "Contato");

    [HttpPost("contato")]
    [HttpPost("PublicPages/Contact")]
    [ValidateAntiForgeryToken]
    public IActionResult Contact(ContactRequest model)
    {
        ViewData["Title"] = "Contato";
        if (!ModelState.IsValid)
        {
            return View("Contact", model);
        }

        logger.LogInformation("Solicitação comercial recebida de {Email} para {Subject}.", model.Email, model.Subject);
        TempData["ContactSuccess"] = "Recebemos sua solicitação. Nossa equipe comercial responderá em até um dia útil.";
        return RedirectToAction(nameof(Contact));
    }

    [HttpGet("privacy")]
    [HttpGet("Privacy")]
    [HttpGet("PublicPages/Privacy")]
    public IActionResult Privacy() => PublicView("Privacy", "Política de Privacidade");

    [Route("whatsapp")]
    public IActionResult WhatsApp() => Redirect("https://wa.me/5591992545353?text=Ol%C3%A1%2C%20quero%20falar%20com%20a%20Valora%20Group%20sobre%20o%20Diagn%C3%B3stico%20Valora%20Insight%E2%84%A2.");

    [Route("entrar")]
    public IActionResult LoginRedirect() => Redirect("/Account/Login");

    private IActionResult PublicView(string viewName, string title)
    {
        try { ViewData["Title"] = title; return View(viewName); }
        catch (Exception ex) { logger.LogError(ex, "Falha ao renderizar página pública {ViewName}.", viewName); throw; }
    }
}
