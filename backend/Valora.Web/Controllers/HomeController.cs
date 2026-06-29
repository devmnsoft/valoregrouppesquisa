using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class HomeController(ILogger<HomeController> logger) : Controller
{
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Home";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar HomeController.Index no Valora.Web.");
            throw;
        }
    }
    public IActionResult Error()
    {
        try
        {
            ViewData["Title"] = "Error";
            return View("Index");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar HomeController.Error no Valora.Web.");
            throw;
        }
    }
}
