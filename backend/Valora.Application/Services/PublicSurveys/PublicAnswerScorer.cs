using Valora.Application.ReadModels;
namespace Valora.Application.Services;
public sealed class PublicAnswerScorer(PublicAnswerNormalizer normalizer)
{
    public IReadOnlyList<ScoredAnswer> Score(IReadOnlyList<QuestionPublicReadModel> questions,IReadOnlyList<FormDimensionReadModel> dimensions,Dictionary<string,object>? answers)
    {
        var normalized = normalizer.Normalize(questions, answers).ToDictionary(x => x.QuestionId);
        var scored = new List<ScoredAnswer>();
        foreach (var q in questions)
        {
            var answer = normalized[q.Id];
            if (q.Required && string.IsNullOrWhiteSpace(answer.AnswerText)) throw new InvalidOperationException($"Pergunta obrigatória sem resposta: {q.Text}");
            var score = answer.NumericValue ?? 0;
            if (score < 0 || score > q.MaxScore) throw new InvalidOperationException($"Resposta fora do range da pergunta: {q.Text}");
            var dimension = dimensions.FirstOrDefault(d => d.Id == q.DimensionId)?.Name ?? "Sem dimensão";
            scored.Add(new(q.Id, dimension, answer.AnswerText, answer.AnswerJson, score, q.MaxScore));
        }
        return scored;
    }
}
