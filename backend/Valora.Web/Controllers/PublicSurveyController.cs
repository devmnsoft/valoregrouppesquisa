using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class PublicSurveyController(ILogger<PublicSurveyController> logger) : Controller
{
    [Route("s/{surveyId}")]
    public IActionResult Take(string surveyId)
    {
        try
        {
            ViewData["Title"] = "Take";
            ViewData["SurveyId"] = surveyId;
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar PublicSurveyController.Take no Valora.Web.");
            throw;
        }
    }
}
