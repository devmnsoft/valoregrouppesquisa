using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
namespace Valora.Api.Controllers;
[ApiController]
public sealed class PublicController(IPublicSurveyService surveys, IPublicResultService results, IWebHostEnvironment env, IConfiguration config, ILogger<PublicController> logger):ControllerBase
{
    private bool LegacyEnabled => env.IsDevelopment() || config.GetValue<bool>("Compatibility:EnableLegacyPublicRoutes");

    [Obsolete("Use POST /public/surveys/{surveyId}/validate. Legacy route is disabled outside Development unless explicitly configured.")]
    [HttpPost("/legacy/public/surveys/{surveyId:guid}/validate")]
    public async Task<IActionResult> Validate(Guid surveyId, ValidateSurveyRequest request)
    {
        if (!LegacyEnabled) return NotFound(new { ok = false, error = "legacy_route_disabled" });
        try { logger.LogWarning("Legacy public validate route used. SurveyId={SurveyId}", surveyId); return Ok(await surveys.ValidateAsync(surveyId, request)); }
        catch (Exception ex) { logger.LogError(ex,"Legacy public validate failed. SurveyId={SurveyId}",surveyId); throw; }
    }

    [Obsolete("Use POST /public/surveys/{surveyId}/responses. Legacy route is disabled outside Development unless explicitly configured.")]
    [HttpPost("/legacy/public/surveys/{surveyId:guid}/responses")]
    public async Task<IActionResult> Submit(Guid surveyId, SubmitSurveyResponseRequest request)
    {
        if (!LegacyEnabled) return NotFound(new { ok = false, error = "legacy_route_disabled" });
        try { logger.LogWarning("Legacy public response route used. SurveyId={SurveyId}", surveyId); return Ok(await surveys.SubmitAsync(surveyId, request)); }
        catch (Exception ex) { logger.LogError(ex,"Legacy public response failed. SurveyId={SurveyId}",surveyId); throw; }
    }

    [Obsolete("Use POST /public/results/{responseId}. Legacy route is disabled outside Development unless explicitly configured.")]
    [HttpPost("/legacy/public/results/{responseId:guid}")]
    public async Task<IActionResult> Result(Guid responseId, PublicResultRequest request)
    {
        if (!LegacyEnabled) return NotFound(new { ok = false, error = "legacy_route_disabled" });
        try { logger.LogWarning("Legacy public result route used. ResponseId={ResponseId}", responseId); return Ok(await results.GetAsync(responseId, request)); }
        catch (Exception ex) { logger.LogError(ex,"Legacy public result failed. ResponseId={ResponseId}",responseId); throw; }
    }
}
