using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class FreeDiagnosticsController(ILogger<FreeDiagnosticsController> logger) : Controller
{
    public IActionResult Index()
    {
        try { ViewData["Title"] = "Diagnósticos gratuitos"; return View(); }
        catch (Exception ex) { logger.LogError(ex, "Falha ao renderizar painel operacional de diagnósticos gratuitos."); throw; }
    }
}
