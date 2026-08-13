using Valora.Application.ReadModels;
namespace Valora.Application.Services;
public sealed class PublicAnswerScorer(PublicAnswerNormalizer normalizer)
{
    public IReadOnlyList<ScoredAnswer> Score(IReadOnlyList<QuestionPublicReadModel> questions,IReadOnlyList<FormDimensionReadModel> dimensions,Dictionary<string,object>? answers)
    {
        var normalized = normalizer.Normalize(questions, answers)
            .GroupBy(answer => answer.QuestionId)
            .ToDictionary(group => group.Key, group => group.Last());
        var scored = new List<ScoredAnswer>();
        foreach (var q in questions)
        {
            if (!normalized.TryGetValue(q.Id, out var answer))
                throw new InvalidOperationException("Não foi possível relacionar a resposta à pergunta do formulário.");
            if (q.Required && string.IsNullOrWhiteSpace(answer.AnswerText)) throw new InvalidOperationException($"Pergunta obrigatória sem resposta: {q.Text}");
            var score = answer.NumericValue ?? 0;
            if (score < 0 || score > q.MaxScore) throw new InvalidOperationException($"Resposta fora do range da pergunta: {q.Text}");
            var dimension = dimensions.FirstOrDefault(d => d.Id == q.DimensionId)?.Name ?? "Sem dimensão";
            scored.Add(new(q.Id, dimension, answer.AnswerText, answer.AnswerJson, score, q.MaxScore));
        }
        return scored;
    }
}
