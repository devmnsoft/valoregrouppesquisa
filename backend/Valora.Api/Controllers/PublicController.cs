using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
namespace Valora.Api.Controllers;
[ApiController]
public sealed class PublicController(IPublicSurveyService surveys, IPublicResultService results):ControllerBase
{
    [HttpPost("/legacy/public/surveys/{surveyId:guid}/validate")]
    public async Task<IActionResult> Validate(Guid surveyId, ValidateSurveyRequest request) => Ok(await surveys.ValidateAsync(surveyId, request));

    [HttpPost("/legacy/public/surveys/{surveyId:guid}/responses")]
    public async Task<IActionResult> Submit(Guid surveyId, SubmitSurveyResponseRequest request) => Ok(await surveys.SubmitAsync(surveyId, request));

    [HttpPost("/legacy/public/results/{responseId:guid}")]
    public async Task<IActionResult> Result(Guid responseId, PublicResultRequest request) => Ok(await results.GetAsync(responseId, request));
}
