using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

/// <summary>
/// Redirecionamentos canônicos das rotas legadas de e-mail para a Central de Comunicações oficial (/Communications).
/// </summary>
public sealed class EmailController : Controller {
    [HttpGet("/Email")]
    public IActionResult Index() => RedirectPermanent("/Communications");

    [HttpGet("/Email/Templates")]
    public IActionResult Templates() => RedirectPermanent("/Communications");

    [HttpGet("/Email/Jobs")]
    public IActionResult Jobs() => RedirectPermanent("/Communications");

    [HttpGet("/Email/Status")]
    public IActionResult Status() => RedirectPermanent("/Communications");
}
