namespace Valora.Application.ValoraBot;

public sealed record ValoraBotAskRequest(string Question, Guid? SessionId = null, string? Context = null);
public sealed record ValoraBotSuggestedActionDto(string Label, string Url, string Kind = "link");
public sealed record ValoraBotAnswerDto(Guid SessionId, Guid MessageId, string Answer, string Intent,
    decimal Confidence, bool NeedsHumanSupport, IReadOnlyList<ValoraBotSuggestedActionDto> SuggestedActions);
public sealed record ValoraBotFeedbackRequest(Guid SessionId, Guid MessageId, bool Helpful, string? Comment = null);
public sealed record ValoraBotKnowledgeDto(Guid Id, string Intent, string QuestionPatterns, string Answer,
    string? ActionLabel, string? ActionUrl, int Priority);

public interface IValoraBotRepository
{
    Task<IReadOnlyList<ValoraBotKnowledgeDto>> GetKnowledgeAsync(CancellationToken ct);
    Task<Guid> EnsureSessionAsync(Guid? sessionId, string? context, CancellationToken ct);
    Task<Guid> SaveExchangeAsync(Guid sessionId, string question, string answer, string intent, decimal confidence,
        bool unanswered, CancellationToken ct);
    Task SaveFeedbackAsync(ValoraBotFeedbackRequest request, CancellationToken ct);
}

public interface IValoraBotService
{
    Task<ValoraBotAnswerDto> AskAsync(ValoraBotAskRequest request, CancellationToken ct = default);
    Task RegisterFeedbackAsync(ValoraBotFeedbackRequest request, CancellationToken ct = default);
}
