using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class SettingsController(ILogger<SettingsController> logger) : Controller
{
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Settings";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar SettingsController.Index no Valora.Web.");
            throw;
        }
    }
}
