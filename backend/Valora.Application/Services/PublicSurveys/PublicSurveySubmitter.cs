using Valora.Application.DTOs;
using Valora.Application.Results;
namespace Valora.Application.Services;
public sealed class PublicSurveySubmitter(PublicSurveyValidator validator,PublicAnswerScorer scorer,ValoraInsightCalculator calculator,PublicResponseTransactionService tx)
{
    public async Task<SubmitSurveyResponseResult> SubmitAsync(Guid surveyId,SubmitSurveyResponseRequest request)
    {
        var data = await validator.ValidateForReadAsync(surveyId,new ValidateSurveyRequest(request.Token,null));
        await validator.ValidateForSubmitAsync(data.Survey.OrganizationId, request, data.Questions);
        var scored = scorer.Score(data.Questions, data.Dims, request.Answers);
        var calc = calculator.Calculate(scored.Select(x => new AnswerScore(x.DimensionName, x.Score)));
        var dims = scored.GroupBy(x=>x.DimensionName).Select(g=>new DimensionScoreInput(g.Key,g.Sum(x=>x.Score),g.Sum(x=>x.MaxScore),g.Sum(x=>x.MaxScore)==0?0:Math.Round(g.Sum(x=>x.Score)/g.Sum(x=>x.MaxScore)*100,2),calc.Level)).ToList();
        return await tx.SaveAsync(data.Survey, request, scored, calc, dims);
    }
}
