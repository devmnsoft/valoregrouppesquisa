using Microsoft.Extensions.Logging;
using Valora.Application.DTOs;
using Valora.Application.Results;
using Valora.Application.Security;

namespace Valora.Application.Services;

public sealed class PublicSurveySubmitter(PublicSurveyValidator validator, PublicAnswerScorer scorer, ValoraInsightCalculator calculator, PublicResponseTransactionService tx, ILogger<PublicSurveySubmitter> logger)
{
    public async Task<SubmitSurveyResponseResult> SubmitAsync(Guid surveyId, SubmitSurveyResponseRequest request)
    {
        try
        {
            logger.LogInformation("Public survey submit started. SurveyId={SurveyId} ParticipantEmail={ParticipantEmail} ParticipantPhone={ParticipantPhone}", surveyId, LogSanitizer.MaskEmail(Val(request.Participant, "email")), LogSanitizer.MaskPhone(Val(request.Participant, "phone")));
            var data = await validator.ValidateForReadAsync(surveyId, new ValidateSurveyRequest(request.Token, null));
            await validator.ValidateForSubmitAsync(data.Survey.OrganizationId, request, data.Questions);
            logger.LogInformation("Public survey validated. SurveyId={SurveyId} OrganizationId={OrganizationId}", surveyId, data.Survey.OrganizationId);
            var scored = scorer.Score(data.Questions, data.Dims, request.Answers);
            logger.LogInformation("Public survey answers scored. SurveyId={SurveyId} AnswerCount={AnswerCount}", surveyId, scored.Count);
            var calc = calculator.Calculate(scored.Select(x => new AnswerScore(x.DimensionName, x.Score)));
            logger.LogInformation("Public survey result calculated. SurveyId={SurveyId} Level={Level}", surveyId, calc.Level);
            var dims = scored.GroupBy(x => x.DimensionName).Select(g => new DimensionScoreInput(g.Key, g.Sum(x => x.Score), g.Sum(x => x.MaxScore), g.Sum(x => x.MaxScore) == 0 ? 0 : Math.Round(g.Sum(x => x.Score) / g.Sum(x => x.MaxScore) * 100, 2), calc.Level)).ToList();
            logger.LogInformation("Public survey transaction called. SurveyId={SurveyId} DimensionCount={DimensionCount}", surveyId, dims.Count);
            var result = await tx.SaveAsync(data.Survey, request, scored, calc, dims);
            logger.LogInformation("Public survey submit succeeded. SurveyId={SurveyId} ResponseId={ResponseId}", surveyId, result.ResponseId);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro na submissão pública. SurveyId={SurveyId}", surveyId);
            throw;
        }
    }

    static string? Val(Dictionary<string, object>? d, string k) => d != null && d.TryGetValue(k, out var v) ? v?.ToString() : null;
}
