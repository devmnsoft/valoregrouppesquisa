using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class EnvironmentStatusController(ILogger<EnvironmentStatusController> logger) : Controller
{
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "EnvironmentStatus";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar EnvironmentStatusController.Index no Valora.Web.");
            throw;
        }
    }
}
