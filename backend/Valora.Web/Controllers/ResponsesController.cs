using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class ResponsesController(ILogger<ResponsesController> logger) : Controller
{
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Responses";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar ResponsesController.Index no Valora.Web.");
            throw;
        }
    }
}
