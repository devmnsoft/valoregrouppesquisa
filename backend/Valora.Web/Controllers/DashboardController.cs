using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class DashboardController(ILogger<DashboardController> logger) : Controller
{
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Dashboard";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar DashboardController.Index no Valora.Web.");
            throw;
        }
    }
}
