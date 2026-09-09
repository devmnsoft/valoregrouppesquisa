using System.Text.Json;
using Valora.Application.DTOs;
using Valora.Application.ReadModels;

namespace Valora.Application.Services;

public sealed class PublicAnswerNormalizer {
    public IReadOnlyList<NormalizedAnswer> Normalize(
        IReadOnlyList<QuestionPublicReadModel> questions,
        IReadOnlyList<QuestionOptionPublicReadModel> options,
        IReadOnlyList<PublicSurveyAnswerRequest>? answers) {
        var supplied = (answers ?? [])
            .GroupBy(answer => answer.QuestionId)
            .ToDictionary(group => group.Key, group => group.Single());

        return questions.Select(question => {
            if (!supplied.TryGetValue(question.Id, out var answer))
                return new NormalizedAnswer(question.Id, null, "null", null);

            var type = NormalizeType(question.Type);
            var text = type switch {
                "scale" or "likert" => answer.ScaleValue?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "single_choice" => LabelFor(answer.OptionId, question.Id, options),
                "multiple_choice" => string.Join(", ", (answer.OptionIds ?? []).Select(id => LabelFor(id, question.Id, options))),
                "number" => answer.NumberValue?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "short_text" or "long_text" => answer.TextValue?.Trim(),
                "yes_no" => answer.BooleanValue is null ? null : answer.BooleanValue.Value ? "Sim" : "Não",
                _ => null
            };
            var numeric = type switch {
                "scale" or "likert" => answer.ScaleValue,
                "single_choice" => ScoreFor(answer.OptionId, question.Id, options),
                "multiple_choice" => AverageScore(answer.OptionIds, question.Id, options),
                "number" => answer.NumberValue,
                "yes_no" => answer.BooleanValue is null ? null : answer.BooleanValue.Value ? question.MaxScore : 0,
                _ => 0
            };
            return new NormalizedAnswer(question.Id, text, JsonSerializer.Serialize(answer), numeric);
        }).ToList();
    }

    public static string NormalizeType(string value) => value.Trim().ToLowerInvariant() switch {
        "scale" or "escala" => "scale",
        "likert" or "likert_1_5" => "likert",
        "single_choice" or "unique_choice" or "unica_escolha" => "single_choice",
        "multiple_choice" or "multipla_escolha" => "multiple_choice",
        "number" or "numero" => "number",
        "short_text" or "text_short" or "texto_curto" => "short_text",
        "long_text" or "text_long" or "texto_longo" => "long_text",
        "yes_no" or "boolean" or "sim_nao" => "yes_no",
        _ => value.Trim().ToLowerInvariant()
    };

    private static string? LabelFor(Guid? optionId, Guid questionId, IReadOnlyList<QuestionOptionPublicReadModel> options) =>
        optionId is null ? null : options.Single(option => option.Id == optionId && option.QuestionId == questionId).Text;

    private static decimal? ScoreFor(Guid? optionId, Guid questionId, IReadOnlyList<QuestionOptionPublicReadModel> options) =>
        optionId is null ? null : options.Single(option => option.Id == optionId && option.QuestionId == questionId).Score;

    private static decimal? AverageScore(IReadOnlyList<Guid>? optionIds, Guid questionId, IReadOnlyList<QuestionOptionPublicReadModel> options) {
        if (optionIds is not { Count: > 0 }) return null;
        var scores = optionIds.Select(id => options.Single(option => option.Id == id && option.QuestionId == questionId).Score ?? 0).ToList();
        return scores.Average();
    }
}
