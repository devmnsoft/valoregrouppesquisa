using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Services;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class SurveysController(ISurveyRepository surveys, AuditService audit) : ControllerBase
{
    [HttpGet("/surveys/public/{token}")]
    public async Task<IActionResult> Public(string token)
    {
        var survey = await surveys.GetPublicByTokenAsync(token);
        return survey is null ? NotFound(new { ok = false }) : Ok(survey);
    }

    [HttpPost("/surveys/{surveyId:guid}/responses")]
    public async Task<IActionResult> Submit(Guid surveyId, SubmitResponseRequest request)
    {
        var id = await surveys.SaveResponseAsync(surveyId, request);
        await audit.LogAsync(new(null, null, "survey.submit_response", "response", id.ToString(), "Resposta recebida pela API."));
        return Created($"/responses/{id}/result", new { id });
    }
}
