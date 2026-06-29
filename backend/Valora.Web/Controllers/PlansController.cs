using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class PlansController(ILogger<PlansController> logger) : Controller
{
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Plans";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar PlansController.Index no Valora.Web.");
            throw;
        }
    }
}
