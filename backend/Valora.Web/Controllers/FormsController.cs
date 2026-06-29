using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class FormsController(ILogger<FormsController> logger) : Controller
{
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Forms";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar FormsController.Index no Valora.Web.");
            throw;
        }
    }
}
