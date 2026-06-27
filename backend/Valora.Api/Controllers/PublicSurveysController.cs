using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class PublicSurveysController(IPublicSurveyService service) : ControllerBase
{
    [HttpPost("/public/surveys/{surveyId:guid}/validate")]
    public async Task<IActionResult> Validate(Guid surveyId, ValidateSurveyRequest request)
    {
        var result = await service.ValidateAsync(surveyId, request);
        return Ok(result);
    }

    [HttpPost("/public/surveys/{surveyId:guid}/responses")]
    public async Task<IActionResult> Submit(Guid surveyId, SubmitSurveyResponseRequest request)
    {
        var result = await service.SubmitAsync(surveyId, request);
        return Ok(result);
    }
}
