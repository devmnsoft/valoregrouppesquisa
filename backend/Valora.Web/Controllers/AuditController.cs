using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class AuditController(ILogger<AuditController> logger) : Controller
{
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Audit";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar AuditController.Index no Valora.Web.");
            throw;
        }
    }
}
