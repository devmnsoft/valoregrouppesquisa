using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class PublicSurveyController : Controller
{
    [Route("s/{surveyId}")]
    public IActionResult Take(string surveyId) { ViewBag.SurveyId = surveyId; return View(); }
}
