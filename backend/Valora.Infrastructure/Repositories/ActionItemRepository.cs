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

    public async Task<PageResult<ActionItemDto>> List(Guid o, Guid u, Guid? planId, bool wide, ActionItemListQuery query, CancellationToken c) {
        var page=query.ValidPage; var pageSize=query.ValidPageSize; var offset=checked((page-1)*pageSize);
        var status=ActionStatuses.Item.Contains(query.Status??"")?query.Status:null;
        var priority=ActionStatuses.Priorities.Contains(query.Priority??"")?query.Priority:null;
        var due=query.Due is "overdue" or "today" or "upcoming" or "none"?query.Due:null;
        var search=string.IsNullOrWhiteSpace(query.Search)?null:query.Search.Trim();
        var where=Scope+" AND (@planId IS NULL OR i.action_plan_id=@planId) AND (@status IS NULL OR i.status=@status) AND (@priority IS NULL OR i.priority=@priority) AND (@responsible IS NULL OR i.responsible_user_id=@responsible) AND (@search IS NULL OR i.title ILIKE '%'||@search||'%' OR i.description ILIKE '%'||@search||'%' OR p.title ILIKE '%'||@search||'%') AND (@due IS NULL OR (@due='overdue' AND i.due_at::date<CURRENT_DATE AND i.status NOT IN ('completed','canceled')) OR (@due='today' AND i.due_at::date=CURRENT_DATE) OR (@due='upcoming' AND i.due_at::date>CURRENT_DATE) OR (@due='none' AND i.due_at IS NULL))";
        using var x=db.Create(); using var q=await x.QueryMultipleAsync(new CommandDefinition($"SELECT count(*)::int FROM valorapesquisa.action_items i JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id WHERE {where}; {Projection} WHERE {where} ORDER BY i.due_at NULLS LAST,i.created_at DESC,i.id LIMIT @pageSize OFFSET @offset",new{o,u,planId,wide,status,priority,responsible=query.ResponsibleUserId,search,due,pageSize,offset},cancellationToken:c));
        var total=await q.ReadSingleAsync<int>(); return new((await q.ReadAsync<ActionItemDto>()).AsList(),page,pageSize,total);
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

    public async Task<PageResult<ActionOptionDto>> ResponsibleOptions(Guid o,OptionQuery query,CancellationToken c){var page=query.ValidPage;var size=query.ValidPageSize;var offset=checked((page-1)*size);var search=string.IsNullOrWhiteSpace(query.Search)?null:query.Search.Trim();const string scope="organization_id=@o AND status='active' AND deleted_at IS NULL AND (@search IS NULL OR name ILIKE '%'||@search||'%')";using var x=db.Create();using var q=await x.QueryMultipleAsync(new CommandDefinition($"SELECT count(*)::int FROM valorapesquisa.users WHERE {scope}; SELECT id Id,name Title,'active' Status,'medium' Priority,NULL::text Context,id ResponsibleUserId,name ResponsibleName FROM valorapesquisa.users WHERE {scope} OR (id=@includeId AND organization_id=@o AND status='active' AND deleted_at IS NULL) ORDER BY name,id LIMIT @size OFFSET @offset",new{o,search,includeId=query.IncludeId,size,offset},cancellationToken:c));var total=await q.ReadSingleAsync<int>();return new((await q.ReadAsync<ActionOptionDto>()).DistinctBy(x=>x.Id).ToList(),page,size,total);}

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

    public async Task<Guid> Create(Guid o, Guid u, CreateActionItemRequest r, CancellationToken c) {
        if(o==Guid.Empty||u==Guid.Empty) throw new UnauthorizedAccessException("Sessão ou organização inválida.");
        var normalized=new { PlanId=r.ActionPlanId,Title=r.Title.Trim(),Description=r.Description.Trim(),OriginType=r.OriginType.Trim().ToLowerInvariant(),r.OriginId,r.DiagnosticId,r.ResultId,RelatedDimension=r.RelatedDimension?.Trim(),RelatedIndexCode=r.RelatedIndexCode?.Trim(),Priority=r.Priority.Trim().ToLowerInvariant(),r.ResponsibleUserId,r.DueAt,EvidenceSummary=r.EvidenceSummary.Trim(),ExpectedOutcome=r.ExpectedOutcome.Trim(),AiRecommendationSummary=r.AiRecommendationSummary?.Trim() };
        var command=r.CommandId.Trim();var fingerprint=CreateFingerprint(o,u,"create-action-item",normalized);
        await using var z=await tx.BeginAsync(c);
        await z.Connection.ExecuteAsync(new CommandDefinition("SELECT pg_advisory_xact_lock(hashtextextended(@lockKey,0))",new{lockKey=$"{o:N}:{u:N}:create-action-item:{command}"},z.Transaction,cancellationToken:c));
        var replay=await z.Connection.QuerySingleOrDefaultAsync<CreateReplay>(new CommandDefinition("SELECT request_hash RequestHash,result_id ResultId FROM valorapesquisa.action_item_create_commands WHERE organization_id=@o AND created_by_user_id=@u AND command_id=@command AND operation='create-action-item'",new{o,u,command},z.Transaction,cancellationToken:c));
        if(replay is not null){if(replay.RequestHash!=fingerprint)throw new ConcurrencyConflictException("A chave de criação já foi usada com outro conteúdo. Revise antes de criar uma nova intenção.");var allowed=await z.Connection.ExecuteScalarAsync<bool>(new CommandDefinition("SELECT EXISTS(SELECT 1 FROM valorapesquisa.action_items i JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id WHERE i.id=@id AND i.organization_id=@o AND i.deleted_at IS NULL AND p.deleted_at IS NULL AND (p.owner_user_id IS NULL OR p.owner_user_id=@u OR i.responsible_user_id=@u))",new{o,u,id=replay.ResultId},z.Transaction,cancellationToken:c));if(!allowed)throw new UnauthorizedAccessException("O resultado da criação não está mais acessível.");await z.CommitAsync();return replay.ResultId;}
        var id=Guid.NewGuid();
        var created=await z.Connection.ExecuteAsync(new CommandDefinition("""
        INSERT INTO valorapesquisa.action_items(id,organization_id,action_plan_id,diagnostic_id,result_id,title,description,origin_type,origin_id,related_dimension,related_index_code,priority,status,responsible_user_id,due_at,evidence_summary,expected_outcome,ai_recommendation_summary)
        SELECT @id,@o,p.id,@DiagnosticId,@ResultId,@Title,@Description,@OriginType,@OriginId,@RelatedDimension,@RelatedIndexCode,@Priority,'pending',@ResponsibleUserId,@DueAt,@EvidenceSummary,@ExpectedOutcome,@AiRecommendationSummary
        FROM valorapesquisa.action_plans p JOIN valorapesquisa.organizations org ON org.id=p.organization_id
        WHERE p.id=@PlanId AND p.organization_id=@o AND p.deleted_at IS NULL AND p.status=ANY(@statuses)
          AND (p.owner_user_id IS NULL OR p.owner_user_id=@u)
          AND EXISTS(SELECT 1 FROM valorapesquisa.users actor WHERE actor.id=@u AND actor.organization_id=@o AND actor.status='active' AND actor.deleted_at IS NULL)
          AND EXISTS(SELECT 1 FROM valorapesquisa.organization_modules om WHERE om.organization_id=@o AND om.module_code='organizational_intelligence' AND om.enabled)
          AND (@ResponsibleUserId IS NULL OR EXISTS(SELECT 1 FROM valorapesquisa.users usr WHERE usr.id=@ResponsibleUserId AND usr.organization_id=@o AND usr.status='active' AND usr.deleted_at IS NULL))
          AND (@DueAt IS NULL OR (@DueAt AT TIME ZONE org.time_zone)::date >= (now() AT TIME ZONE org.time_zone)::date)
          AND (@DiagnosticId IS NULL OR EXISTS(SELECT 1 FROM valorapesquisa.diagnostics d WHERE d.id=@DiagnosticId AND d.organization_id=@o AND d.deleted_at IS NULL))
          AND (@ResultId IS NULL OR EXISTS(SELECT 1 FROM valorapesquisa.results rs WHERE rs.id=@ResultId AND rs.organization_id=@o))
        """,new{id,o,u,normalized.PlanId,normalized.DiagnosticId,normalized.ResultId,normalized.Title,normalized.Description,normalized.OriginType,normalized.OriginId,normalized.RelatedDimension,normalized.RelatedIndexCode,normalized.Priority,normalized.ResponsibleUserId,normalized.DueAt,normalized.EvidenceSummary,normalized.ExpectedOutcome,normalized.AiRecommendationSummary,statuses=new[]{"draft","proposed","approved","in_execution"}},z.Transaction,cancellationToken:c));
        if(created!=1)throw new ConflictAppException("Não foi possível criar: confirme módulo contratado, acesso de escrita ao plano, estado, responsável, origem e prazo no fuso da organização.");
        await Event(z,o,u,id,"action_created",normalized.Title,normalized.EvidenceSummary,c);
        await z.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.action_item_create_commands(organization_id,created_by_user_id,command_id,operation,request_hash,result_id) VALUES(@o,@u,@command,'create-action-item',@fingerprint,@id)",new{o,u,command,fingerprint,id},z.Transaction,cancellationToken:c));
        await z.CommitAsync();return id;
    }

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

    private sealed record CreateReplay(string RequestHash,Guid ResultId);
    private sealed record ItemState(string Status,long Version,string Title);
    private sealed record ReplayState(string? Operation,string? IntentHash,long? IntentVersion,Guid? ChangedBy);
    private static string CreateFingerprint(Guid organization,Guid actor,string operation,object payload){var json=System.Text.Json.JsonSerializer.Serialize(new{organization,actor,operation,payload});return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();}
    private static string Fingerprint(string operation,long version,int? progress,string? reason,string? result,string? evidence){static string N(string? value)=>value?.Trim()??"";var value=string.Join("\n",operation,version.ToString(CultureInfo.InvariantCulture),progress?.ToString(CultureInfo.InvariantCulture)??"",N(reason),N(result),N(evidence));return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();}
    static Task Event(IUnitOfWork z, Guid o, Guid u, Guid id, string type, string title, string evidence, CancellationToken c) => z.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.journey_events(organization_id,event_type,title,description,source_type,source_id,impact_level,evidence_summary,occurred_at,created_by_user_id) VALUES(@o,@type,@title,'Registro automático da execução organizacional.','action',@id,'medium',@evidence,now(),@u)", new { o, u, id, type, title, evidence }, z.Transaction, cancellationToken: c));
    private static DynamicParameters OptionParameters(Guid o,Guid u,bool wide,string? search,int offset,int size) { var p=new DynamicParameters();p.Add("o",o,DbType.Guid);p.Add("u",u,DbType.Guid);p.Add("wide",wide,DbType.Boolean);p.Add("search",string.IsNullOrWhiteSpace(search)?null:search.Trim(),DbType.String);p.Add("itemStatuses",new[]{"pending","in_progress","blocked","overdue"});p.Add("planStatuses",new[]{"draft","proposed","approved","in_execution"});p.Add("offset",offset,DbType.Int32);p.Add("size",size,DbType.Int32);return p; }
}
