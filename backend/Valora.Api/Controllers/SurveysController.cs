using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class SurveysController(ISurveyRepository surveys) : ControllerBase
{
    [HttpGet("/surveys/public/{token}")]
    public async Task<IActionResult> Public(string token)
    {
        var survey = await surveys.GetPublicByTokenAsync(token);
        return survey is null ? NotFound(new { ok = false }) : Ok(survey);
    }

    [HttpPost("/surveys/{surveyId:guid}/responses")]
    public IActionResult Submit(Guid surveyId)
    {
        // Kept as an explicit tombstone so old clients cannot bypass public-token,
        // plan-limit, answer validation and LGPD-consent enforcement.
        return StatusCode(StatusCodes.Status410Gone, new
        {
            ok = false,
            code = "LEGACY_SURVEY_SUBMISSION_DISABLED",
            message = "Use POST /public/surveys/{surveyId}/responses com token público e consentimento LGPD.",
            surveyId,
            correlationId = HttpContext.TraceIdentifier
        });
    }
}
