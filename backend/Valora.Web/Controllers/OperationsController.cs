using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class OperationsController(ILogger<OperationsController> logger) : Controller
{
    [Route("Operations")]
    [Route("Operations/Index")]
    public IActionResult Index()
    {
        logger.LogInformation("Operations panel rendered by Valora.Web.");
        ViewData["Title"] = "Saúde operacional";
        return View();
    }
}
