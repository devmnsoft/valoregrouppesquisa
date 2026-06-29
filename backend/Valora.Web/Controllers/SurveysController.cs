using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class SurveysController(ILogger<SurveysController> logger) : Controller
{
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Surveys";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar SurveysController.Index no Valora.Web.");
            throw;
        }
    }
    public IActionResult PublicLinks()
    {
        try
        {
            ViewData["Title"] = "PublicLinks";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar SurveysController.PublicLinks no Valora.Web.");
            throw;
        }
    }
}
