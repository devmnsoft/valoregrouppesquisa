using Microsoft.AspNetCore.Mvc;
using Valora.Application.DTOs;
using Valora.Application.Services;
using Valora.Application.Contracts;
using Valora.Application.Results;
namespace Valora.Api.Controllers;

[ApiController]
public sealed class PublicController(ISurveyRepository surveys, IResponseRepository responses, AuditService audit):ControllerBase
{
    private static readonly Guid DemoSurveyGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly string[] DemoDimensions = ["Cultura e Propósito", "Gestão e Governança", "Liderança", "Pessoas e Talentos", "Resultados e Crescimento"];
    private static bool IsDemo(string surveyId, string? token) =>
        string.Equals(surveyId, "demo-valora-insight", StringComparison.OrdinalIgnoreCase)
        || string.Equals(surveyId, DemoSurveyGuid.ToString(), StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "demo-public-token", StringComparison.OrdinalIgnoreCase);

    [HttpPost("/legacy/public/surveys/{surveyId}/validate")]
    public async Task<IActionResult> Validate(string surveyId, PublicSurveyValidateRequest request)
    {
        if (IsDemo(surveyId, request.Token)) return Ok(new { ok=true, data=BuildDemoSurvey(request.Org), survey=BuildDemoSurvey(request.Org).survey, form=BuildDemoSurvey(request.Org).form, company=BuildDemoSurvey(request.Org).company, lgpd=new { text="Termo LGPD demonstrativo para homologação local Valora Insight™." } });
        var survey = await surveys.GetPublicByTokenAsync(request.Token);
        return survey is null ? NotFound(new { ok=false, message="Pesquisa pública não encontrada, expirada ou token inválido.", code="PUBLIC_SURVEY_NOT_FOUND" }) : Ok(new { ok=true, survey, company=new { slug=request.Org }, form=new { id="pending-backfill", questions=Array.Empty<object>() } });
    }

    [HttpPost("/legacy/public/surveys/{surveyId}/responses")]
    public async Task<IActionResult> Submit(string surveyId, PublicSurveySubmitRequest request)
    {
        if (!request.LgpdConsent) return BadRequest(new { ok=false, message="Consentimento LGPD obrigatório.", code="LGPD_REQUIRED" });
        if (IsDemo(surveyId, request.Token))
        {
            var responseId = Guid.NewGuid();
            var result = CalculateDemoResult(request.Answers);
            await audit.LogAsync(new(null,null,"public_survey.demo_submit","response",responseId.ToString(),"Resposta demo Valora Insight™ recebida pela API PostgreSQL."));
            return Ok(new { ok=true, responseId, resultToken=$"result_{responseId:N}", emailStatus=request.CommunicationConsent ? "pending" : "cancelled", certificate=new { responseId, status="metadata-ready", validationCode=$"VAL-{responseId:N}"[..14] }, communication=new { resultEmail=new { status=request.CommunicationConsent ? "pending" : "cancelled" } }, result });
        }
        if (!Guid.TryParse(surveyId, out var parsedSurveyId)) return BadRequest(new { ok=false, message="Identificador da pesquisa inválido.", code="INVALID_SURVEY_ID" });
        var id = await surveys.SaveResponseAsync(parsedSurveyId, new SubmitResponseRequest(
            request.Participant.TryGetValue("name", out var name) ? Convert.ToString(name) : null,
            request.Participant.TryGetValue("email", out var email) ? Convert.ToString(email) : null,
            request.Answers));
        await audit.LogAsync(new(null,null,"public_survey.submit","response",id.ToString(),"Resposta pública recebida pela API PostgreSQL."));
        return Ok(new { ok=true, responseId=id, resultToken=$"result_{id:N}", emailStatus=request.CommunicationConsent ? "pending" : "cancelled", certificate=new { responseId=id, status="metadata-ready" } });
    }

    [HttpPost("/legacy/public/results/{responseId}")]
    public async Task<IActionResult> Result(string responseId, PublicResultRequest request)
    {
        if (responseId.StartsWith("demo", StringComparison.OrdinalIgnoreCase) || (request.ResultToken ?? string.Empty).StartsWith("result_", StringComparison.OrdinalIgnoreCase))
        {
            var id = Guid.TryParse(responseId, out var parsed) ? parsed : Guid.NewGuid();
            var result = CalculateDemoResult(null);
            return Ok(new { ok=true, response=BuildDemoResponse(id, result), result, survey=BuildDemoSurvey(null).survey, company=BuildDemoSurvey(null).company, certificate=new { responseId=id, status="metadata-ready", validationCode=$"VAL-{id:N}"[..14] }, communication=new { resultEmail=new { status="pending" } } });
        }
        if (!Guid.TryParse(responseId, out var parsedResponseId)) return BadRequest(new { ok=false, message="Identificador do resultado inválido.", code="INVALID_RESPONSE_ID" });
        var resultDb = await responses.GetResultAsync(parsedResponseId);
        return resultDb is null ? NotFound(new { ok=false, message="Resultado não encontrado.", code="RESULT_NOT_FOUND" }) : Ok(new { ok=true, result=resultDb, survey=new {}, company=new {}, certificate=new { responseId=parsedResponseId, status="metadata-ready" } });
    }

    private static dynamic BuildDemoSurvey(string? org) => new { survey=new { id="demo-valora-insight", title="Diagnóstico Valora Insight™", status="active", token="demo-public-token", lgpdRequired=true }, company=new { id="org_demo", name="Valora Group Demo", publicName="Valora Group Demo", slug=org ?? "valora-demo" }, form=new { id="form_demo_valora_insight", name="Diagnóstico Valora Insight™", timeMin=8, questions=Enumerable.Range(1,25).Select(i => new { id=$"q{i}", text=$"Pergunta Valora Insight {i}", type="scale", required=true, maxScore=5, dimensionName=DemoDimensions[(i-1)/5] }) } };
    private static object BuildDemoResponse(Guid id, ValoraInsightResult result) => new { id, rawScore=result.TotalScore, maxScore=result.MaxScore, percentage=decimal.Round(result.TotalScore / result.MaxScore * 100, 2), normalized5=decimal.Round(result.TotalScore / 25, 2), maturityLabel=result.Level, level=new { label=result.Level, description=result.StrategicTruth, recommendation=result.NextLevel }, valoraInsight=result, createdAt=DateTimeOffset.UtcNow };
    private static ValoraInsightResult CalculateDemoResult(Dictionary<string,object>? answers)
    {
        var scores = DemoDimensions.SelectMany((d,di)=>Enumerable.Range(1,5).Select(q=>new AnswerScore(d, answers is null ? (di==0&&q<=2?2:3) : Math.Clamp(Convert.ToDecimal(answers.GetValueOrDefault($"q{di*5+q}", 3)), 1, 5))));
        return new ValoraInsightCalculator().Calculate(scores);
    }
}
