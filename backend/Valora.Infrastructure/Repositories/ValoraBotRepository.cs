using Dapper;
using Valora.Application.Contracts;
using Valora.Application.ValoraBot;

namespace Valora.Infrastructure.Repositories;

public sealed class ValoraBotRepository(IDbConnectionFactory connections, IDbTransactionFactory transactions) : IValoraBotRepository
{
    public async Task<IReadOnlyList<ValoraBotKnowledgeDto>> GetKnowledgeAsync(CancellationToken ct)
    {
        const string sql = "SELECT id,intent,question_patterns QuestionPatterns,answer,action_label ActionLabel,action_url ActionUrl,priority FROM valorapesquisa.valorabot_knowledge_base WHERE is_active ORDER BY priority DESC,intent";
        using var connection = connections.Create();
        return (await connection.QueryAsync<ValoraBotKnowledgeDto>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
    }

    public async Task<Guid> EnsureSessionAsync(Guid? sessionId, string? context, CancellationToken ct)
    {
        var id = sessionId ?? Guid.NewGuid();
        const string sql = "INSERT INTO valorapesquisa.valorabot_sessions(id,context,last_activity_at) VALUES(@id,@context,now()) ON CONFLICT(id) DO UPDATE SET context=coalesce(EXCLUDED.context,valorabot_sessions.context),last_activity_at=now()";
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { id, context }, cancellationToken: ct));
        return id;
    }

    public async Task<Guid> SaveExchangeAsync(Guid sessionId, string question, string answer, string intent, decimal confidence, bool unanswered, CancellationToken ct)
    {
        await using var unit = await transactions.BeginAsync(ct);
        var answerId = Guid.NewGuid();
        try
        {
            const string messages = "INSERT INTO valorapesquisa.valorabot_messages(session_id,role,content,intent,confidence) VALUES(@sessionId,'user',@question,NULL,NULL); INSERT INTO valorapesquisa.valorabot_messages(id,session_id,role,content,intent,confidence) VALUES(@answerId,@sessionId,'assistant',@answer,@intent,@confidence)";
            await unit.Connection.ExecuteAsync(new CommandDefinition(messages, new { sessionId, question, answerId, answer, intent, confidence }, unit.Transaction, cancellationToken: ct));
            if (unanswered) await unit.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.valorabot_unanswered_questions(session_id,question,normalized_question) VALUES(@sessionId,@question,lower(trim(@question)))", new { sessionId, question }, unit.Transaction, cancellationToken: ct));
            await unit.CommitAsync();
            return answerId;
        }
        catch { await unit.RollbackAsync(); throw; }
    }

    public async Task SaveFeedbackAsync(ValoraBotFeedbackRequest request, CancellationToken ct)
    {
        const string sql = "INSERT INTO valorapesquisa.valorabot_feedback(session_id,message_id,helpful,comment) SELECT @SessionId,@MessageId,@Helpful,@Comment WHERE EXISTS(SELECT 1 FROM valorapesquisa.valorabot_messages WHERE id=@MessageId AND session_id=@SessionId) ON CONFLICT(session_id,message_id) DO UPDATE SET helpful=EXCLUDED.helpful,comment=EXCLUDED.comment,updated_at=now()";
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, request, cancellationToken: ct));
    }
}
