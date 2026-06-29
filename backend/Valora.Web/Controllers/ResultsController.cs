using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class ResultsController(ILogger<ResultsController> logger) : Controller
{
    [Route("r/{responseId}")]
    public IActionResult Public(string responseId)
    {
        try
        {
            ViewData["Title"] = "Public";
            ViewData["ResponseId"] = responseId;
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar ResultsController.Public no Valora.Web.");
            throw;
        }
    }
    public IActionResult Details(string id)
    {
        try
        {
            ViewData["Title"] = "Details";
            ViewData["ResponseId"] = id;
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar ResultsController.Details no Valora.Web.");
            throw;
        }
    }
}
