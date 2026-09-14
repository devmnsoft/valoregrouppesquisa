using Dapper;
using System.Data;
using Valora.Application.ActionCenter;
using Valora.Application.Contracts;
using Valora.Application.Exceptions;
using Valora.Application.Workspace;

namespace Valora.Infrastructure.Repositories;

public sealed class ActionItemRepository(IDbConnectionFactory db, IDbTransactionFactory tx) : IActionItemRepository {
    internal const string Select = "SELECT id,action_plan_id ActionPlanId,title,description,origin_type OriginType,origin_id OriginId,related_dimension RelatedDimension,related_index_code RelatedIndexCode,priority,status,responsible_user_id ResponsibleUserId,due_at DueAt,completed_at CompletedAt,progress_percent ProgressPercent,evidence_summary EvidenceSummary,expected_outcome ExpectedOutcome,completion_evidence CompletionEvidence,ai_recommendation_summary AiRecommendationSummary,created_at CreatedAt,version Version FROM valorapesquisa.action_items";

    public async Task<IReadOnlyList<ActionItemDto>> List(Guid o, Guid? p, CancellationToken c) {
        using var x = db.Create();
        return (await x.QueryAsync<ActionItemDto>(new CommandDefinition(Select + " WHERE organization_id=@o AND deleted_at IS NULL AND (@p IS NULL OR action_plan_id=@p) ORDER BY due_at NULLS LAST,created_at DESC", new { o, p }, cancellationToken: c))).ToList();
    }

    public async Task<PageResult<ActionOptionDto>> Options(Guid o, Guid u, bool wide, OptionQuery query, CancellationToken c) {
        var page = query.ValidPage; var size = query.ValidPageSize;
        var offset = checked((page - 1) * size);
        var args = OptionParameters(o, u, wide, query.Search, offset, size);
        const string scope = "i.organization_id=@o AND i.deleted_at IS NULL AND p.deleted_at IS NULL AND i.status=ANY(@itemStatuses) AND p.status=ANY(@planStatuses) AND (@wide OR i.responsible_user_id IS NULL OR i.responsible_user_id=@u OR p.owner_user_id=@u) AND (@search IS NULL OR i.title ILIKE '%'||@search||'%' OR p.title ILIKE '%'||@search||'%')";
        var sql = $"""
            SELECT count(*)::int FROM valorapesquisa.action_items i
            JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id WHERE {scope};
            SELECT i.id Id,i.title Title,i.status Status,i.priority Priority,p.title Context,i.responsible_user_id ResponsibleUserId,u2.name ResponsibleName
            FROM valorapesquisa.action_items i JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id
            LEFT JOIN valorapesquisa.users u2 ON u2.id=i.responsible_user_id AND u2.organization_id=i.organization_id AND u2.deleted_at IS NULL
            WHERE {scope} ORDER BY i.title,i.id LIMIT @size OFFSET @offset;
            """;
        using var x = db.Create(); using var result = await x.QueryMultipleAsync(new CommandDefinition(sql, args, cancellationToken: c));
        var total = await result.ReadSingleAsync<int>(); var rows = (await result.ReadAsync<ActionOptionDto>()).AsList(); return new(rows, page, size, total);
    }

    public async Task<ActionItemDto?> Get(Guid o, Guid id, CancellationToken c) { using var x = db.Create(); return await x.QuerySingleOrDefaultAsync<ActionItemDto>(new CommandDefinition(Select + " WHERE organization_id=@o AND id=@id AND deleted_at IS NULL", new { o, id }, cancellationToken: c)); }

    public async Task<ActionItemDetailsDto?> Details(Guid o, Guid id, int historyPage, CancellationToken c) {
        var page = Math.Max(1, historyPage); const int size = 10; var offset = checked((page - 1) * size);
        const string sql = """
            SELECT i.id,i.action_plan_id ActionPlanId,i.title,i.description,i.origin_type OriginType,i.origin_id OriginId,i.related_dimension RelatedDimension,i.related_index_code RelatedIndexCode,i.priority,i.status,i.responsible_user_id ResponsibleUserId,i.due_at DueAt,i.completed_at CompletedAt,i.progress_percent ProgressPercent,i.evidence_summary EvidenceSummary,i.expected_outcome ExpectedOutcome,i.completion_evidence CompletionEvidence,i.ai_recommendation_summary AiRecommendationSummary,i.created_at CreatedAt,i.version Version,u.name ResponsibleName,p.title PlanTitle,i.metadata_json->>'completionResult' CompletionResult
            FROM valorapesquisa.action_items i JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id
            LEFT JOIN valorapesquisa.users u ON u.id=i.responsible_user_id AND u.organization_id=i.organization_id AND u.deleted_at IS NULL
            WHERE i.organization_id=@o AND i.id=@id AND i.deleted_at IS NULL;
            SELECT count(*)::int FROM valorapesquisa.action_item_status_history h JOIN valorapesquisa.action_items i ON i.id=h.action_item_id WHERE i.organization_id=@o AND i.id=@id AND i.deleted_at IS NULL;
            SELECT h.id,h.from_status FromStatus,h.to_status ToStatus,h.progress_percent ProgressPercent,h.reason,u.name AuthorName,h.changed_at ChangedAt
            FROM valorapesquisa.action_item_status_history h JOIN valorapesquisa.action_items i ON i.id=h.action_item_id
            LEFT JOIN valorapesquisa.users u ON u.id=h.changed_by_user_id AND u.organization_id=i.organization_id
            WHERE i.organization_id=@o AND i.id=@id AND i.deleted_at IS NULL ORDER BY h.changed_at DESC,h.id DESC LIMIT @size OFFSET @offset;
            """;
        using var x = db.Create(); using var q = await x.QueryMultipleAsync(new CommandDefinition(sql, new { o, id, size, offset }, cancellationToken: c));
        var item = await q.ReadSingleOrDefaultAsync<ActionItemDto>(); if (item is null) return null;
        var count = await q.ReadSingleAsync<int>(); var history = (await q.ReadAsync<ActionHistoryDto>()).AsList();
        var operational = item.Status is "pending" or "in_progress" or "overdue";
        return new(item, history, page, Math.Max(1, (count + size - 1) / size), operational, operational, operational);
    }

    public async Task<Guid> Create(Guid o, Guid u, CreateActionItemRequest r, CancellationToken c) { var id = Guid.NewGuid(); await using var z = await tx.BeginAsync(c); var created = await z.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.action_items(id,organization_id,action_plan_id,diagnostic_id,result_id,title,description,origin_type,origin_id,related_dimension,related_index_code,priority,status,responsible_user_id,due_at,evidence_summary,expected_outcome,ai_recommendation_summary) SELECT @id,@o,p.id,@DiagnosticId,@ResultId,@Title,@Description,@OriginType,@OriginId,@RelatedDimension,@RelatedIndexCode,@Priority,'pending',@ResponsibleUserId,@DueAt,@EvidenceSummary,@ExpectedOutcome,@AiRecommendationSummary FROM valorapesquisa.action_plans p WHERE p.id=@ActionPlanId AND p.organization_id=@o AND p.deleted_at IS NULL AND p.status=ANY(@statuses)", new { id, o, r.ActionPlanId, r.DiagnosticId, r.ResultId, r.Title, r.Description, r.OriginType, r.OriginId, r.RelatedDimension, r.RelatedIndexCode, r.Priority, r.ResponsibleUserId, r.DueAt, r.EvidenceSummary, r.ExpectedOutcome, r.AiRecommendationSummary, statuses = new[] { "draft", "proposed", "approved", "in_execution" } }, z.Transaction, cancellationToken: c)); if (created != 1) throw new ConflictAppException("O plano não está disponível para receber atividades."); await Event(z, o, u, id, "action_created", r.Title, r.EvidenceSummary, c); await z.CommitAsync(); return id; }

    public Task UpdateProgress(Guid o, Guid u, Guid id, ProgressActionRequest r, CancellationToken c) => Transition(o,u,id,r.Version,r.CommandId,new[]{"pending","in_progress","overdue"},r.ProgressPercent == 0 ? "pending" : "in_progress",r.ProgressPercent,r.Note,null,null,c);
    public Task Block(Guid o, Guid u, Guid id, TransitionActionRequest r, CancellationToken c) => Transition(o,u,id,r.Version,r.CommandId,new[]{"pending","in_progress","overdue"},"blocked",null,r.Reason,null,null,c);
    public Task Complete(Guid o, Guid u, Guid id, CompleteActionRequest r, CancellationToken c) => Transition(o,u,id,r.Version,r.CommandId,new[]{"pending","in_progress","overdue"},"completed",100,r.Result,r.Result,r.Evidence,c);

    private async Task Transition(Guid o, Guid u, Guid id, long version, string commandId, string[] allowed, string next, int? progress, string? reason, string? result, string? evidence, CancellationToken c) {
        await using var z = await tx.BeginAsync(c);
        var replay = await z.Connection.ExecuteScalarAsync<bool>(new CommandDefinition("SELECT EXISTS(SELECT 1 FROM valorapesquisa.action_item_status_history h JOIN valorapesquisa.action_items i ON i.id=h.action_item_id WHERE i.organization_id=@o AND i.id=@id AND h.command_id=@commandId)", new { o,id,commandId },z.Transaction,cancellationToken:c));
        if (replay) { await z.CommitAsync(); return; }
        var current = await z.Connection.QuerySingleOrDefaultAsync<ItemState>(new CommandDefinition("SELECT i.status,i.version,i.title FROM valorapesquisa.action_items i JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id WHERE i.id=@id AND i.organization_id=@o AND i.deleted_at IS NULL AND p.deleted_at IS NULL FOR UPDATE OF i",new{o,id},z.Transaction,cancellationToken:c));
        if (current is null) throw new KeyNotFoundException("Atividade não encontrada no escopo autorizado.");
        if (current.Version != version) throw new ConcurrencyConflictException("A atividade foi alterada. Revise os dados atuais antes de reenviar.");
        if (!allowed.Contains(current.Status)) throw new ConflictAppException($"A transição de {current.Status} para {next} não é permitida.");
        var affected = await z.Connection.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.action_items SET status=@next,progress_percent=COALESCE(@progress,progress_percent),completed_at=CASE WHEN @next='completed' THEN now() ELSE completed_at END,completion_evidence=COALESCE(@evidence,completion_evidence),metadata_json=CASE WHEN @result IS NULL THEN metadata_json ELSE metadata_json||jsonb_build_object('completionResult',@result) END,version=version+1,updated_at=now() WHERE id=@id AND organization_id=@o AND version=@version AND deleted_at IS NULL",new{o,id,version,next,progress,evidence,result},z.Transaction,cancellationToken:c));
        if (affected != 1) throw new ConcurrencyConflictException("A atividade foi alterada durante a operação.");
        if (progress is < 100) await z.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.action_item_checkins(action_item_id,progress_percent,note,created_by_user_id,command_id) VALUES(@id,@progress,@reason,@u,@commandId)",new{id,progress,reason,u,commandId},z.Transaction,cancellationToken:c));
        await z.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.action_item_status_history(action_item_id,from_status,to_status,progress_percent,reason,changed_by_user_id,command_id) VALUES(@id,@from,@next,@progress,@reason,@u,@commandId)",new{id,from=current.Status,next,progress,reason,u,commandId},z.Transaction,cancellationToken:c));
        if (next == "completed") await Event(z,o,u,id,"action_completed",current.Title,evidence!,c);
        await z.CommitAsync();
    }

    private sealed record ItemState(string Status,long Version,string Title);
    static Task Event(IUnitOfWork z, Guid o, Guid u, Guid id, string type, string title, string evidence, CancellationToken c) => z.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.journey_events(organization_id,event_type,title,description,source_type,source_id,impact_level,evidence_summary,occurred_at,created_by_user_id) VALUES(@o,@type,@title,'Registro automático da execução organizacional.','action',@id,'medium',@evidence,now(),@u)", new { o, u, id, type, title, evidence }, z.Transaction, cancellationToken: c));
    private static DynamicParameters OptionParameters(Guid o,Guid u,bool wide,string? search,int offset,int size) { var p=new DynamicParameters();p.Add("o",o,DbType.Guid);p.Add("u",u,DbType.Guid);p.Add("wide",wide,DbType.Boolean);p.Add("search",string.IsNullOrWhiteSpace(search)?null:search.Trim(),DbType.String);p.Add("itemStatuses",new[]{"pending","in_progress","blocked","overdue"});p.Add("planStatuses",new[]{"draft","proposed","approved","in_execution"});p.Add("offset",offset,DbType.Int32);p.Add("size",size,DbType.Int32);return p; }
}
