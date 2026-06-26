using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
namespace Valora.Api.Controllers;
[ApiController]
public sealed class PublicSurveysController(IPublicSurveyService service) : ControllerBase
{
    [HttpPost("/public/surveys/{surveyId:guid}/validate")]
    public async Task<IActionResult> Validate(Guid surveyId, ValidateSurveyRequest request) => Ok(await service.ValidateAsync(surveyId, request));

    [HttpPost("/public/surveys/{surveyId:guid}/responses")]
    public async Task<IActionResult> Submit(Guid surveyId, SubmitSurveyResponseRequest request) => Ok(await service.SubmitAsync(surveyId, request));
}
