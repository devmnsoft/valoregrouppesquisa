using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class MigrationController(ILogger<MigrationController> logger) : Controller
{
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Migration";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar MigrationController.Index no Valora.Web.");
            throw;
        }
    }
}
