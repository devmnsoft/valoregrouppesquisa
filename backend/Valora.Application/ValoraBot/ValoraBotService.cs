using System.Globalization;
using System.Text;

namespace Valora.Application.ValoraBot;

public sealed class ValoraBotService(IValoraBotRepository repository) : IValoraBotService
{
    private const string Fallback = "Ainda não encontrei uma orientação segura para essa pergunta. Posso ajudar com Valora Insight™, diagnóstico gratuito, resultado, certificado, LGPD, planos, acesso, Dashboard, Heatmap, Benchmark, Action, Evolution, Journey ou Executive Report. Se preferir, fale com a equipe oficial pelo WhatsApp +55 91 99254-5353.";

    public async Task<ValoraBotAnswerDto> AskAsync(ValoraBotAskRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Question)) throw new ArgumentException("Digite uma pergunta para o ValoraBot.");
        var question = request.Question.Trim();
        if (question.Length > 1000) throw new ArgumentException("A pergunta deve ter no máximo 1.000 caracteres.");
        var normalized = Normalize(question);
        var knowledge = await repository.GetKnowledgeAsync(ct);
        var match = knowledge.Select(item => new { Item = item, Score = Score(normalized, item.QuestionPatterns) })
            .OrderByDescending(x => x.Score).ThenByDescending(x => x.Item.Priority).FirstOrDefault();
        var answered = match is { Score: > 0 };
        var item = answered ? match!.Item : null;
        var answer = item?.Answer ?? Fallback;
        var intent = item?.Intent ?? "fallback";
        var confidence = answered ? Math.Min(1m, .55m + match!.Score * .1m) : 0m;
        var actions = item?.ActionUrl is { Length: > 0 }
            ? new[] { new ValoraBotSuggestedActionDto(item.ActionLabel ?? "Saiba mais", item.ActionUrl) }
            : new[] { new ValoraBotSuggestedActionDto("Falar no WhatsApp", "https://wa.me/5591992545353", "support") };
        var sessionId = await repository.EnsureSessionAsync(request.SessionId, request.Context, ct);
        var messageId = await repository.SaveExchangeAsync(sessionId, question, answer, intent, confidence, !answered, ct);
        return new(sessionId, messageId, answer, intent, confidence, !answered, actions);
    }

    public Task RegisterFeedbackAsync(ValoraBotFeedbackRequest request, CancellationToken ct = default) => repository.SaveFeedbackAsync(request, ct);

    private static int Score(string question, string patterns) => patterns.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(Normalize).Where(x => x.Length > 1).Count(question.Contains);

    private static string Normalize(string value)
    {
        var text = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(text.Length);
        foreach (var c in text) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) builder.Append(char.IsLetterOrDigit(c) ? c : ' ');
        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
