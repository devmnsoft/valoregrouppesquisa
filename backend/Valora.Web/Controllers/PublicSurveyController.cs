using Microsoft.AspNetCore.Mvc;
using Valora.Web.Models;

namespace Valora.Web.Controllers;

public sealed class PublicSurveyController(ILogger<PublicSurveyController> logger) : Controller
{
    [HttpGet("r/{token}")]
    [HttpGet("r/{token}/start")]
    [HttpGet("r/{token}/questions")]
    [HttpGet("r/{token}/review")]
    [HttpGet("r/{token}/completed")]
    public IActionResult Respondent(string token)
    {
        var step = Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (step == token) step = "start";
        var model = new RespondentExperienceViewModel { Token = token, Step = step ?? "start" };
        if (!TryValidateModel(model)) return View("RespondentUnavailable", model);
        return View("Respondent", model);
    }

    [Route("s/{surveyId}")]
    [Route("public/surveys/{surveyId}")]
    [Route("pesquisa/{surveyId}")]
    [Route("pesquisa/{surveyId}/responder")]
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
