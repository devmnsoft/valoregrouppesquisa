using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class EnvironmentStatusController(ILogger<EnvironmentStatusController> logger) : Controller
{
    [HttpGet("/SystemHealth")]
    [HttpGet("/EnvironmentStatus")]
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Saúde do Sistema";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar EnvironmentStatusController.Index no Valora.Web.");
            throw;
        }
    }
}
