using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Results;
using Valora.Application.Services;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class PublicSurveysController(ISurveyRepository surveys, AuditService audit) : ControllerBase
{
    private static readonly Guid DemoSurveyGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly string[] DemoDimensions = ["Cultura e Propósito", "Gestão e Governança", "Liderança", "Pessoas e Talentos", "Resultados e Crescimento"];

    [HttpPost("/public/surveys/{surveyId}/validate")]
    public async Task<IActionResult> Validate(string surveyId, PublicSurveyValidateRequest request)
    {
        if (IsDemo(surveyId, request.Token))
        {
            var data = BuildDemoSurvey(request.Org);
            return Ok(new { ok = true, data, data.survey, data.form, data.company, lgpd = new { text = "Termo LGPD demonstrativo para homologação local Valora Insight™." } });
        }

        var survey = await surveys.GetPublicByTokenAsync(request.Token);
        return survey is null
            ? NotFound(new { ok = false, message = "Pesquisa pública não encontrada, expirada ou token inválido.", code = "PUBLIC_SURVEY_NOT_FOUND" })
            : Ok(new { ok = true, survey, company = new { slug = request.Org }, form = new { id = "pending-backfill", questions = Array.Empty<object>() } });
    }

    [HttpPost("/public/surveys/{surveyId}/responses")]
    public async Task<IActionResult> Submit(string surveyId, PublicSurveySubmitRequest request)
    {
        if (!request.LgpdConsent) return BadRequest(new { ok = false, message = "Consentimento LGPD obrigatório.", code = "LGPD_REQUIRED" });
        if (IsDemo(surveyId, request.Token))
        {
            var responseId = Guid.NewGuid();
            var result = CalculateDemoResult(request.Answers);
            await audit.LogAsync(new(null, null, "public_survey.demo_submit", "response", responseId.ToString(), "Resposta demo Valora Insight™ recebida pela API PostgreSQL."));
            return Ok(new { ok = true, responseId, resultToken = $"result_{responseId:N}", emailStatus = request.CommunicationConsent ? "pending" : "cancelled", certificate = new { responseId, status = "metadata-ready", validationCode = $"VAL-{responseId:N}"[..14] }, result });
        }

        if (!Guid.TryParse(surveyId, out var parsedSurveyId)) return BadRequest(new { ok = false, message = "Identificador da pesquisa inválido.", code = "INVALID_SURVEY_ID" });
        var id = await surveys.SaveResponseAsync(parsedSurveyId, new SubmitResponseRequest(
            request.Participant.GetValueOrDefault("name")?.ToString(),
            request.Participant.GetValueOrDefault("email")?.ToString(),
            request.Answers));
        await audit.LogAsync(new(null, null, "public_survey.submit", "response", id.ToString(), "Resposta pública recebida pela API PostgreSQL."));
        return Ok(new { ok = true, responseId = id, resultToken = $"result_{id:N}", emailStatus = request.CommunicationConsent ? "pending" : "cancelled", certificate = new { responseId = id, status = "metadata-ready" } });
    }

    private static bool IsDemo(string surveyId, string? token) =>
        string.Equals(surveyId, "demo-valora-insight", StringComparison.OrdinalIgnoreCase)
        || string.Equals(surveyId, DemoSurveyGuid.ToString(), StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "demo-public-token", StringComparison.OrdinalIgnoreCase);

    private static dynamic BuildDemoSurvey(string? org) => new { survey = new { id = "demo-valora-insight", title = "Diagnóstico Valora Insight™", status = "active", token = "demo-public-token", lgpdRequired = true }, company = new { id = "org_demo", name = "Valora Group Demo", publicName = "Valora Group Demo", slug = org ?? "valora-demo" }, form = new { id = "form_demo_valora_insight", name = "Diagnóstico Valora Insight™", timeMin = 8, questions = Enumerable.Range(1, 25).Select(i => new { id = $"q{i}", text = $"Pergunta Valora Insight {i}", type = "scale", required = true, maxScore = 5, dimensionName = DemoDimensions[(i - 1) / 5] }) } };
    private static ValoraInsightResult CalculateDemoResult(Dictionary<string, object>? answers)
    {
        var scores = DemoDimensions.SelectMany((d, di) => Enumerable.Range(1, 5).Select(q => new AnswerScore(d, answers is null ? (di == 0 && q <= 2 ? 2 : 3) : Math.Clamp(Convert.ToDecimal(answers.GetValueOrDefault($"q{di * 5 + q}", 3)), 1, 5))));
        return new ValoraInsightCalculator().Calculate(scores);
    }
}
