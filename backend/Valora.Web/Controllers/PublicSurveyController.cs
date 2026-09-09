using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Valora.Web.Models;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[AllowAnonymous]
public sealed class PublicSurveyController(ILogger<PublicSurveyController> logger, IBffApiClient api,
    PublicAccessSessionStore publicSessions) : Controller
{
    [HttpGet("r/{token}")]
    [HttpGet("r/{token}/start")]
    [HttpGet("r/{token}/questions")]
    [HttpGet("r/{token}/review")]
    [HttpGet("r/{token}/completed")]
    public IActionResult Respondent(string token)
    {
        Response.StatusCode = StatusCodes.Status410Gone;
        return View("RespondentUnavailable", new RespondentExperienceViewModel { Token = "unavailable", Step = "start" });
    }

    [Route("s/{surveyId}")]
    [Route("public/surveys/{surveyId}")]
    [Route("pesquisa/{surveyId}")]
    [Route("pesquisa/{surveyId}/responder")]
    public async Task<IActionResult> Take(Guid surveyId, string? token, CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                using var validation = await api.SendAsync(HttpMethod.Post, $"/public/surveys/{surveyId}/validate",
                    new { token }, string.Empty, HttpContext.TraceIdentifier, cancellationToken);
                if (!validation.IsSuccessStatusCode)
                {
                    Response.StatusCode = (int)validation.StatusCode;
                    return View("RespondentUnavailable", new RespondentExperienceViewModel { Token = "unavailable", Step = "start" });
                }
                await publicSessions.WriteAsync(HttpContext, "survey", surveyId, token, cancellationToken);
                return Redirect($"/public/surveys/{surveyId}");
            }
            ViewData["Title"] = "Take";
            ViewData["SurveyId"] = surveyId.ToString();
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar PublicSurveyController.Take no Valora.Web.");
            throw;
        }
    }
}
