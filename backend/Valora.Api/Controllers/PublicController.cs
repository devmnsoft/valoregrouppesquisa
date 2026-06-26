using Microsoft.AspNetCore.Mvc;
using Valora.Application.DTOs;
using Valora.Application.Services;
using Valora.Application.Contracts;
namespace Valora.Api.Controllers;
[ApiController]
public sealed class PublicController(ISurveyRepository surveys, AuditService audit):ControllerBase
{
    [HttpPost("/public/surveys/{surveyId:guid}/validate")]
    public async Task<IActionResult> Validate(Guid surveyId, PublicSurveyValidateRequest request)
    {
        var survey = await surveys.GetPublicByTokenAsync(request.Token);
        return survey is null ? NotFound(new { ok=false, message="Pesquisa pública não encontrada, expirada ou token inválido." }) : Ok(new { ok=true, survey, company=new { slug=request.Org }, form=new { id="pending-backfill" } });
    }

    [HttpPost("/public/surveys/{surveyId:guid}/responses")]
    public async Task<IActionResult> Submit(Guid surveyId, PublicSurveySubmitRequest request)
    {
        if (!request.LgpdConsent) return BadRequest(new { ok=false, message="Consentimento LGPD obrigatório." });
        var id = await surveys.SaveResponseAsync(surveyId, new SubmitResponseRequest(
            request.Participant.TryGetValue("name", out var name) ? Convert.ToString(name) : null,
            request.Participant.TryGetValue("email", out var email) ? Convert.ToString(email) : null,
            request.Answers));
        await audit.LogAsync(new(null,null,"public_survey.submit","response",id.ToString(),"Resposta pública recebida pela API PostgreSQL."));
        return Ok(new { ok=true, responseId=id, resultToken=$"result_{id:N}", emailStatus=request.CommunicationConsent ? "pending" : "cancelled" });
    }

    [HttpPost("/public/results/{responseId:guid}")]
    public async Task<IActionResult> Result(Guid responseId, PublicResultRequest request)
    {
        var result = await surveys.GetResultAsync(responseId);
        return result is null ? NotFound(new { ok=false, message="Resultado não encontrado." }) : Ok(new { ok=true, result, survey=new {}, company=new {}, certificate=new { responseId, status="metadata-ready" } });
    }
}
