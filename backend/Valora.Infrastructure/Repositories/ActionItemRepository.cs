using Dapper;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Valora.Application.ActionCenter;
using Valora.Application.Contracts;
using Valora.Application.Exceptions;
using Valora.Application.Workspace;

namespace Valora.Infrastructure.Repositories;

public sealed class ActionItemRepository(IDbConnectionFactory db, IDbTransactionFactory tx) : IActionItemRepository {
    internal const string Projection = "SELECT i.id,i.action_plan_id ActionPlanId,i.title,i.description,i.origin_type OriginType,i.origin_id OriginId,i.related_dimension RelatedDimension,i.related_index_code RelatedIndexCode,i.priority,i.status,i.responsible_user_id ResponsibleUserId,i.due_at DueAt,i.completed_at CompletedAt,i.progress_percent ProgressPercent,i.evidence_summary EvidenceSummary,i.expected_outcome ExpectedOutcome,i.completion_evidence CompletionEvidence,i.ai_recommendation_summary AiRecommendationSummary,i.created_at CreatedAt,i.version Version,u2.name ResponsibleName,p.title PlanTitle,i.metadata_json->>'completionResult' CompletionResult FROM valorapesquisa.action_items i JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id LEFT JOIN valorapesquisa.users u2 ON u2.id=i.responsible_user_id AND u2.organization_id=i.organization_id AND u2.deleted_at IS NULL";
    internal const string Scope = "i.organization_id=@o AND i.deleted_at IS NULL AND p.deleted_at IS NULL AND (@wide OR i.responsible_user_id IS NULL OR i.responsible_user_id=@u OR p.owner_user_id=@u)";

    public async Task<PageResult<ActionItemDto>> List(Guid o, Guid u, Guid? planId, bool wide, int page, int pageSize, string? status, CancellationToken c) {
        page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,1,50);var offset=checked((page-1)*pageSize);var normalized=ActionStatuses.Item.Contains(status??"")?status:null;
        var where=Scope+" AND (@planId IS NULL OR i.action_plan_id=@planId) AND (@status IS NULL OR i.status=@status)";
        using var x=db.Create();using var q=await x.QueryMultipleAsync(new CommandDefinition($"SELECT count(*)::int FROM valorapesquisa.action_items i JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id WHERE {where}; {Projection} WHERE {where} ORDER BY i.due_at NULLS LAST,i.created_at DESC,i.id LIMIT @pageSize OFFSET @offset",new{o,u,planId,wide,status=normalized,pageSize,offset},cancellationToken:c));
        var total=await q.ReadSingleAsync<int>();return new((await q.ReadAsync<ActionItemDto>()).AsList(),page,pageSize,total);
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

    public async Task<IReadOnlyList<ActionOptionDto>> ResponsibleOptions(Guid o,CancellationToken c){using var x=db.Create();return (await x.QueryAsync<ActionOptionDto>(new CommandDefinition("SELECT id Id,name Title,'active' Status,'medium' Priority,email Context,id ResponsibleUserId,name ResponsibleName FROM valorapesquisa.users WHERE organization_id=@o AND status='active' AND deleted_at IS NULL ORDER BY name,id LIMIT 200",new{o},cancellationToken:c))).AsList();}

    public async Task<ActionItemDto?> Get(Guid o, Guid u, Guid id, bool wide, CancellationToken c) { using var x=db.Create();return await x.QuerySingleOrDefaultAsync<ActionItemDto>(new CommandDefinition(Projection+$" WHERE {Scope} AND i.id=@id",new{o,u,id,wide},cancellationToken:c)); }

    public async Task<ActionItemDetailsDto?> Details(Guid o, Guid u, Guid id, bool wide, bool canManage, bool canComplete, int historyPage, CancellationToken c) {
        var page = Math.Max(1, historyPage); const int size = 10; var offset = checked((page - 1) * size);
        const string sql = """
            SELECT i.id,i.action_plan_id ActionPlanId,i.title,i.description,i.origin_type OriginType,i.origin_id OriginId,i.related_dimension RelatedDimension,i.related_index_code RelatedIndexCode,i.priority,i.status,i.responsible_user_id ResponsibleUserId,i.due_at DueAt,i.completed_at CompletedAt,i.progress_percent ProgressPercent,i.evidence_summary EvidenceSummary,i.expected_outcome ExpectedOutcome,i.completion_evidence CompletionEvidence,i.ai_recommendation_summary AiRecommendationSummary,i.created_at CreatedAt,i.version Version,u.name ResponsibleName,p.title PlanTitle,i.metadata_json->>'completionResult' CompletionResult
            FROM valorapesquisa.action_items i JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id
            LEFT JOIN valorapesquisa.users u ON u.id=i.responsible_user_id AND u.organization_id=i.organization_id AND u.deleted_at IS NULL
            WHERE i.organization_id=@o AND i.id=@id AND i.deleted_at IS NULL AND p.deleted_at IS NULL AND (@wide OR i.responsible_user_id IS NULL OR i.responsible_user_id=@u OR p.owner_user_id=@u);
            SELECT count(*)::int FROM valorapesquisa.action_item_status_history h JOIN valorapesquisa.action_items i ON i.id=h.action_item_id WHERE i.organization_id=@o AND i.id=@id AND i.deleted_at IS NULL;
            SELECT h.id,h.from_status FromStatus,h.to_status ToStatus,h.progress_percent ProgressPercent,h.reason,u.name AuthorName,h.changed_at ChangedAt
            FROM valorapesquisa.action_item_status_history h JOIN valorapesquisa.action_items i ON i.id=h.action_item_id
            LEFT JOIN valorapesquisa.users u ON u.id=h.changed_by_user_id AND u.organization_id=i.organization_id
            WHERE i.organization_id=@o AND i.id=@id AND i.deleted_at IS NULL AND EXISTS(SELECT 1 FROM valorapesquisa.action_plans p WHERE p.id=i.action_plan_id AND p.organization_id=i.organization_id AND p.deleted_at IS NULL AND (@wide OR i.responsible_user_id IS NULL OR i.responsible_user_id=@u OR p.owner_user_id=@u)) ORDER BY h.changed_at DESC,h.id DESC LIMIT @size OFFSET @offset;
            """;
        using var x = db.Create(); using var q = await x.QueryMultipleAsync(new CommandDefinition(sql, new { o,u,id,wide,size,offset }, cancellationToken: c));
        var item = await q.ReadSingleOrDefaultAsync<ActionItemDto>(); if (item is null) return null;
        var count = await q.ReadSingleAsync<int>(); var history = (await q.ReadAsync<ActionHistoryDto>()).AsList();
        var operational = item.Status is "pending" or "in_progress" or "overdue";
        return new(item, history, page, Math.Max(1, (count + size - 1) / size), canManage&&operational, canManage&&operational, canManage&&item.Status=="blocked", canComplete&&operational);
    }

    public async Task<Guid> Create(Guid o, Guid u, CreateActionItemRequest r, CancellationToken c) { var id=Guid.NewGuid();await using var z=await tx.BeginAsync(c);var created=await z.Connection.ExecuteAsync(new CommandDefinition("""
        INSERT INTO valorapesquisa.action_items(id,organization_id,action_plan_id,diagnostic_id,result_id,title,description,origin_type,origin_id,related_dimension,related_index_code,priority,status,responsible_user_id,due_at,evidence_summary,expected_outcome,ai_recommendation_summary)
        SELECT @id,@o,p.id,@DiagnosticId,@ResultId,@Title,@Description,@OriginType,@OriginId,@RelatedDimension,@RelatedIndexCode,@Priority,'pending',@ResponsibleUserId,@DueAt,@EvidenceSummary,@ExpectedOutcome,@AiRecommendationSummary
        FROM valorapesquisa.action_plans p WHERE p.id=@ActionPlanId AND p.organization_id=@o AND p.deleted_at IS NULL AND p.status=ANY(@statuses)
        AND (@ResponsibleUserId IS NULL OR EXISTS(SELECT 1 FROM valorapesquisa.users usr WHERE usr.id=@ResponsibleUserId AND usr.organization_id=@o AND usr.status='active' AND usr.deleted_at IS NULL))
        AND NOT EXISTS(SELECT 1 FROM valorapesquisa.action_items existing WHERE existing.organization_id=@o AND existing.action_plan_id=p.id AND existing.deleted_at IS NULL AND lower(btrim(existing.title))=lower(btrim(@Title)))
        """,new{id,o,r.ActionPlanId,r.DiagnosticId,r.ResultId,Title=r.Title.Trim(),r.Description,r.OriginType,r.OriginId,r.RelatedDimension,r.RelatedIndexCode,r.Priority,r.ResponsibleUserId,r.DueAt,EvidenceSummary=r.EvidenceSummary.Trim(),ExpectedOutcome=r.ExpectedOutcome.Trim(),r.AiRecommendationSummary,statuses=new[]{"draft","proposed","approved","in_execution"}},z.Transaction,cancellationToken:c));if(created!=1)throw new ConflictAppException("O plano foi encerrado, o responsável está inativo ou já existe uma atividade com este título.");await Event(z,o,u,id,"action_created",r.Title,r.EvidenceSummary,c);await z.CommitAsync();return id; }

    public Task UpdateProgress(Guid o,Guid u,Guid id,bool wide,ProgressActionRequest r,CancellationToken c)=>Transition(o,u,id,wide,r.Version,r.CommandId,"progress",new[]{"pending","in_progress","overdue"},r.ProgressPercent==0?"pending":"in_progress",r.ProgressPercent,r.Note,null,null,c);
    public Task Block(Guid o,Guid u,Guid id,bool wide,TransitionActionRequest r,CancellationToken c)=>Transition(o,u,id,wide,r.Version,r.CommandId,"block",new[]{"pending","in_progress","overdue"},"blocked",null,r.Reason,null,null,c);
    public Task Resume(Guid o,Guid u,Guid id,bool wide,TransitionActionRequest r,CancellationToken c)=>Transition(o,u,id,wide,r.Version,r.CommandId,"resume",new[]{"blocked"},"in_progress",null,r.Reason,null,null,c);
    public Task Complete(Guid o,Guid u,Guid id,bool wide,CompleteActionRequest r,CancellationToken c)=>Transition(o,u,id,wide,r.Version,r.CommandId,"complete",new[]{"pending","in_progress","overdue"},"completed",100,r.Result,r.Result,r.Evidence,c);

    private async Task Transition(Guid o, Guid u, Guid id, bool wide, long version, string commandId, string operation, string[] allowed, string next, int? progress, string? reason, string? result, string? evidence, CancellationToken c) {
        await using var z = await tx.BeginAsync(c);
        var fingerprint=Fingerprint(operation,version,progress,reason,result,evidence);
        await z.Connection.ExecuteAsync(new CommandDefinition("SELECT pg_advisory_xact_lock(hashtextextended(@lockKey,0))",new{lockKey=$"{o:N}:{id:N}:{commandId}"},z.Transaction,cancellationToken:c));
        var current = await z.Connection.QuerySingleOrDefaultAsync<ItemState>(new CommandDefinition("SELECT i.status,i.version,i.title FROM valorapesquisa.action_items i JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id WHERE i.id=@id AND i.organization_id=@o AND i.deleted_at IS NULL AND p.deleted_at IS NULL AND (@wide OR i.responsible_user_id IS NULL OR i.responsible_user_id=@u OR p.owner_user_id=@u) FOR UPDATE OF i",new{o,u,id,wide},z.Transaction,cancellationToken:c));
        if (current is null) throw new KeyNotFoundException("Atividade não encontrada no escopo autorizado.");
        var replay=await z.Connection.QuerySingleOrDefaultAsync<ReplayState>(new CommandDefinition("SELECT h.operation,h.intent_hash IntentHash,h.intent_version IntentVersion,h.changed_by_user_id ChangedBy FROM valorapesquisa.action_item_status_history h WHERE h.action_item_id=@id AND h.command_id=@commandId",new{id,commandId},z.Transaction,cancellationToken:c));
        if(replay is not null){if(replay.Operation==operation&&replay.IntentHash==fingerprint&&replay.IntentVersion==version&&replay.ChangedBy==u){await z.CommitAsync();return;}throw new ConcurrencyConflictException("A chave da operação já foi usada com outra intenção.");}
        if (current.Version != version) throw new ConcurrencyConflictException("A atividade foi alterada. Revise os dados atuais antes de reenviar.");
        if (!allowed.Contains(current.Status)) throw new ConflictAppException($"A transição de {current.Status} para {next} não é permitida.");
        var affected = await z.Connection.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.action_items SET status=@next,progress_percent=COALESCE(@progress,progress_percent),completed_at=CASE WHEN @next='completed' THEN now() ELSE completed_at END,completion_evidence=COALESCE(@evidence,completion_evidence),metadata_json=CASE WHEN @result IS NULL THEN metadata_json ELSE metadata_json||jsonb_build_object('completionResult',@result) END,version=version+1,updated_at=now() WHERE id=@id AND organization_id=@o AND version=@version AND deleted_at IS NULL",new{o,id,version,next,progress,evidence,result},z.Transaction,cancellationToken:c));
        if (affected != 1) throw new ConcurrencyConflictException("A atividade foi alterada durante a operação.");
        if (progress is < 100) await z.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.action_item_checkins(action_item_id,progress_percent,note,created_by_user_id,command_id) VALUES(@id,@progress,@reason,@u,@commandId)",new{id,progress,reason,u,commandId},z.Transaction,cancellationToken:c));
        await z.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.action_item_status_history(action_item_id,from_status,to_status,progress_percent,reason,changed_by_user_id,command_id,operation,intent_hash,intent_version) VALUES(@id,@from,@next,@progress,@reason,@u,@commandId,@operation,@fingerprint,@version)",new{id,from=current.Status,next,progress,reason,u,commandId,operation,fingerprint,version},z.Transaction,cancellationToken:c));
        if (next == "completed") await Event(z,o,u,id,"action_completed",current.Title,evidence!,c);
        await z.CommitAsync();
    }

    private sealed record ItemState(string Status,long Version,string Title);
    private sealed record ReplayState(string? Operation,string? IntentHash,long? IntentVersion,Guid? ChangedBy);
    private static string Fingerprint(string operation,long version,int? progress,string? reason,string? result,string? evidence){static string N(string? value)=>value?.Trim()??"";var value=string.Join("\n",operation,version.ToString(CultureInfo.InvariantCulture),progress?.ToString(CultureInfo.InvariantCulture)??"",N(reason),N(result),N(evidence));return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();}
    static Task Event(IUnitOfWork z, Guid o, Guid u, Guid id, string type, string title, string evidence, CancellationToken c) => z.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.journey_events(organization_id,event_type,title,description,source_type,source_id,impact_level,evidence_summary,occurred_at,created_by_user_id) VALUES(@o,@type,@title,'Registro automático da execução organizacional.','action',@id,'medium',@evidence,now(),@u)", new { o, u, id, type, title, evidence }, z.Transaction, cancellationToken: c));
    private static DynamicParameters OptionParameters(Guid o,Guid u,bool wide,string? search,int offset,int size) { var p=new DynamicParameters();p.Add("o",o,DbType.Guid);p.Add("u",u,DbType.Guid);p.Add("wide",wide,DbType.Boolean);p.Add("search",string.IsNullOrWhiteSpace(search)?null:search.Trim(),DbType.String);p.Add("itemStatuses",new[]{"pending","in_progress","blocked","overdue"});p.Add("planStatuses",new[]{"draft","proposed","approved","in_execution"});p.Add("offset",offset,DbType.Int32);p.Add("size",size,DbType.Int32);return p; }
}
