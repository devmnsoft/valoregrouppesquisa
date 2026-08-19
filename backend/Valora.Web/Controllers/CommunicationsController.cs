using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class CommunicationsController(ILogger<CommunicationsController> logger) : Controller
{
    [HttpGet("/Communications")]
    [HttpGet("/Notifications")]
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Communications";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar CommunicationsController.Index no Valora.Web.");
            throw;
        }
    }
}
