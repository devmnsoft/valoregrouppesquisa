using System.Security.Cryptography;
using System.Text;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.Exceptions;
using Valora.Application.Workspace;

namespace Valora.Infrastructure.Repositories;

public sealed class WorkspaceRepository(IDbConnectionFactory connections) : IWorkspaceRepository {
    private const string Columns = "w.id Id,w.item_type ItemType,w.title Title,w.summary Summary,w.status Status,w.priority Priority,w.due_at DueAt,w.owner_user_id OwnerUserId,w.source_type SourceType,w.source_id SourceId,w.route Route,w.created_at CreatedAt";
    private const string Visible = "(@wide OR w.owner_user_id=@u OR w.owner_user_id IS NULL)";
    public Task<IReadOnlyList<WorkspaceItemDto>> MyDayAsync(Guid o, Guid u, bool wide, CancellationToken ct) => Query($"SELECT {Columns},EXISTS(SELECT 1 FROM valorapesquisa.user_pinned_items p WHERE p.organization_id=w.organization_id AND p.user_id=@u AND p.workspace_item_id=w.id) IsPinned FROM valorapesquisa.workspace_items w WHERE w.organization_id=@o AND w.status NOT IN ('completed','cancelled') AND {Visible} AND (w.due_at::date<=CURRENT_DATE OR w.priority='critical' OR w.item_type IN ('approval','evidence','governance_alert')) ORDER BY CASE w.priority WHEN 'critical' THEN 0 WHEN 'high' THEN 1 WHEN 'medium' THEN 2 ELSE 3 END, w.due_at NULLS LAST,w.id LIMIT 100", new { o, u, wide }, ct);
    public Task<IReadOnlyList<WorkspaceItemDto>> RecentAsync(Guid o, Guid u, bool wide, CancellationToken ct) => Query($"SELECT {Columns},EXISTS(SELECT 1 FROM valorapesquisa.user_pinned_items p WHERE p.organization_id=w.organization_id AND p.user_id=@u AND p.workspace_item_id=w.id) IsPinned FROM valorapesquisa.user_recent_items r JOIN valorapesquisa.workspace_items w ON w.id=r.workspace_item_id AND w.organization_id=r.organization_id WHERE r.organization_id=@o AND r.user_id=@u AND {Visible} ORDER BY r.opened_at DESC,w.id LIMIT 12", new { o, u, wide }, ct);
    public Task<IReadOnlyList<WorkspaceItemDto>> PinnedAsync(Guid o, Guid u, bool wide, CancellationToken ct) => Query($"SELECT {Columns},true IsPinned FROM valorapesquisa.user_pinned_items p JOIN valorapesquisa.workspace_items w ON w.id=p.workspace_item_id AND w.organization_id=p.organization_id WHERE p.organization_id=@o AND p.user_id=@u AND {Visible} ORDER BY p.created_at DESC,w.id", new { o, u, wide }, ct);
    public async Task PinAsync(Guid o, Guid u, Guid id, bool wide, CancellationToken ct) { using var c = connections.Create(); var changed = await c.ExecuteAsync(new CommandDefinition($"INSERT INTO valorapesquisa.user_pinned_items(organization_id,user_id,workspace_item_id) SELECT @o,@u,w.id FROM valorapesquisa.workspace_items w WHERE w.id=@id AND w.organization_id=@o AND {Visible} ON CONFLICT(organization_id,user_id,workspace_item_id) DO UPDATE SET workspace_item_id=EXCLUDED.workspace_item_id", new { o, u, id, wide }, cancellationToken: ct)); if (changed == 0) throw new UnauthorizedAccessException("Item indisponível no contexto autorizado."); }
    public async Task UnpinAsync(Guid o, Guid u, Guid id, CancellationToken ct) { using var c = connections.Create(); await c.ExecuteAsync(new CommandDefinition("DELETE FROM valorapesquisa.user_pinned_items WHERE organization_id=@o AND user_id=@u AND workspace_item_id=@id", new { o, u, id }, cancellationToken: ct)); }
    public async Task RecordOpenAsync(Guid o, Guid u, Guid id, bool wide, CancellationToken ct) { using var c = connections.Create(); var changed = await c.ExecuteAsync(new CommandDefinition($"INSERT INTO valorapesquisa.user_recent_items(organization_id,user_id,workspace_item_id,opened_at) SELECT @o,@u,w.id,now() FROM valorapesquisa.workspace_items w WHERE w.organization_id=@o AND w.id=@id AND {Visible} ON CONFLICT(organization_id,user_id,workspace_item_id) DO UPDATE SET opened_at=EXCLUDED.opened_at", new { o, u, id, wide }, cancellationToken: ct)); if (changed == 0) throw new UnauthorizedAccessException("Item indisponível no contexto autorizado."); }
    private async Task<IReadOnlyList<WorkspaceItemDto>> Query(string sql, object parameters, CancellationToken ct) { using var c = connections.Create(); return (await c.QueryAsync<WorkspaceItemDto>(new CommandDefinition(sql, parameters, cancellationToken: ct))).AsList(); }
}

public sealed class GlobalSearchRepository(IDbConnectionFactory connections) : IGlobalSearchRepository {
    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(Guid o, Guid u, string term, bool wide, CancellationToken ct) { using var c = connections.Create(); const string sql = "SELECT id Id,result_type ResultType,title Title,description Description,route Route,updated_at UpdatedAt FROM valorapesquisa.global_search_index WHERE organization_id=@o AND search_vector @@ websearch_to_tsquery('simple',@term) AND (@wide OR allowed_user_id IS NULL OR allowed_user_id=@u) ORDER BY ts_rank(search_vector,websearch_to_tsquery('simple',@term)) DESC,updated_at DESC LIMIT 60"; return (await c.QueryAsync<SearchResultDto>(new CommandDefinition(sql, new { o, u, term, wide }, cancellationToken: ct))).AsList(); }
    public async Task RecordAsync(Guid o, Guid u, string term, int count, CancellationToken ct) { using var c = connections.Create(); await c.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.global_search_events(organization_id,user_id,term,result_count) VALUES(@o,@u,@term,@count)", new { o, u, term, count }, cancellationToken: ct)); }
}
public sealed class QuickActionRepository(IDbConnectionFactory connections) : IQuickActionRepository {
    public async Task<IReadOnlyList<QuickActionDto>> ListAsync(Guid o, CancellationToken ct) { using var c = connections.Create(); return (await c.QueryAsync<QuickActionDto>(new CommandDefinition("SELECT code Code,label Label,description Description,route Route,icon Icon,sort_order SortOrder FROM valorapesquisa.quick_actions WHERE enabled=true ORDER BY sort_order,code", cancellationToken: ct))).AsList(); }
    public async Task RecordAsync(Guid o, Guid u, string code, CancellationToken ct) { using var c = connections.Create(); await c.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.quick_action_events(organization_id,user_id,action_code) VALUES(@o,@u,@code)", new { o, u, code }, cancellationToken: ct)); }
}
public sealed class ExecutivePriorityRepository(IDbConnectionFactory connections) : IExecutivePriorityRepository {
    private const string Projection = "p.id Id,p.title Title,p.description Description,p.status Status,p.priority Priority,p.owner_user_id OwnerUserId,u.name OwnerName,p.due_at DueAt,p.source_type SourceType,p.source_id SourceId,p.progress_percent ProgressPercent,p.updated_at UpdatedAt";
    private const string Visible = "(@wide OR p.owner_user_id=@user OR p.owner_user_id IS NULL)";

    public async Task<IReadOnlyList<ExecutivePriorityDto>> ListAsync(Guid o, Guid user, bool wide, CancellationToken ct) =>
        (await ListAsync(o, user, wide, new PriorityListQuery(1, 8, "active"), ct)).Items;

    public async Task<PageResult<ExecutivePriorityDto>> ListAsync(Guid o, Guid user, bool wide, PriorityListQuery query, CancellationToken ct) {
        using var c = connections.Create();
        var page=query.ValidPage; var size=query.ValidPageSize; var offset=(page-1)*size;
        var where=$"p.organization_id=@o AND {Visible} AND (@status IS NULL OR p.status=@status) AND (@priority IS NULL OR p.priority=@priority) AND (@owner IS NULL OR p.owner_user_id=@owner) AND (@due IS NULL OR (@due='overdue' AND p.due_at<CURRENT_TIMESTAMP AND p.status='active') OR (@due='today' AND p.due_at::date=CURRENT_DATE) OR (@due='none' AND p.due_at IS NULL))";
        var parameters=new {o,user,wide,status=Clean(query.Status),priority=Clean(query.Priority),owner=query.OwnerUserId,due=Clean(query.Due),size,offset};
        var total=await c.ExecuteScalarAsync<int>(new CommandDefinition($"SELECT count(*) FROM valorapesquisa.executive_priorities p WHERE {where}",parameters,cancellationToken:ct));
        var sql=$"SELECT {Projection} FROM valorapesquisa.executive_priorities p LEFT JOIN valorapesquisa.users u ON u.id=p.owner_user_id AND u.organization_id=p.organization_id WHERE {where} ORDER BY CASE p.status WHEN 'active' THEN 0 WHEN 'completed' THEN 1 ELSE 2 END,CASE p.priority WHEN 'critical' THEN 0 WHEN 'high' THEN 1 WHEN 'medium' THEN 2 ELSE 3 END,p.due_at NULLS LAST,p.updated_at DESC,p.id LIMIT @size OFFSET @offset";
        var rows=(await c.QueryAsync<ExecutivePriorityDto>(new CommandDefinition(sql,parameters,cancellationToken:ct))).AsList();
        return new(rows,page,size,total);
    }

    public async Task<PriorityDetailsDto?> GetAsync(Guid o, Guid user, Guid id, bool wide, bool canManage, CancellationToken ct) {
        using var c = connections.Create();
        var priority = await c.QuerySingleOrDefaultAsync<ExecutivePriorityDto>(new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.executive_priorities p LEFT JOIN valorapesquisa.users u ON u.id=p.owner_user_id AND u.organization_id=p.organization_id WHERE p.organization_id=@o AND p.id=@id AND {Visible}", new { o, user, id, wide }, cancellationToken: ct));
        if (priority is null) return null;
        var history = (await c.QueryAsync<PriorityUpdateDto>(new CommandDefinition("SELECT x.id Id,x.progress_percent ProgressPercent,x.note Note,x.event_type EventType,x.created_by CreatedBy,coalesce(u.name,'Usuário indisponível') AuthorName,x.created_at CreatedAt FROM valorapesquisa.executive_priority_updates x LEFT JOIN valorapesquisa.users u ON u.id=x.created_by AND u.organization_id=x.organization_id WHERE x.organization_id=@o AND x.priority_id=@id ORDER BY x.created_at,x.id", new { o, id }, cancellationToken: ct))).AsList();
        return Details(priority, history, canManage);
    }

    public async Task<ExecutivePriorityDto> CreateAsync(Guid o, Guid user, CreatePriorityRequest r, CancellationToken ct) {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction();
        await ValidateReferences(c, tx, o, user, r.OwnerUserId, r.SourceType, r.SourceId);
        var id = Guid.NewGuid();
        var priority = await c.QuerySingleAsync<ExecutivePriorityDto>(new CommandDefinition($"INSERT INTO valorapesquisa.executive_priorities(id,organization_id,title,description,priority,owner_user_id,due_at,source_type,source_id,created_by) VALUES(@id,@o,@Title,@Description,@Priority,@OwnerUserId,@DueAt,@SourceType,@SourceId,@user) RETURNING {Projection.Replace("p.", "").Replace("u.name OwnerName", "NULL::text OwnerName")}", new { id, o, user, Title=r.Title.Trim(), Description=Clean(r.Description), r.Priority, r.OwnerUserId, r.DueAt, SourceType=Clean(r.SourceType), r.SourceId }, tx, cancellationToken: ct));
        await c.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.executive_priority_updates(organization_id,priority_id,progress_percent,note,event_type,created_by) VALUES(@o,@id,0,'Prioridade criada','created',@user); INSERT INTO valorapesquisa.workspace_items(id,organization_id,item_type,title,summary,status,priority,due_at,owner_user_id,source_type,source_id,route) VALUES(@id,@o,'priority',@Title,@Description,'active',@Priority,@DueAt,@OwnerUserId,@SourceType,@SourceId,'/Workspace/Priorities')", new { o, id, user, Title=r.Title.Trim(), Description=Clean(r.Description), r.Priority, r.OwnerUserId, r.DueAt, SourceType=Clean(r.SourceType), r.SourceId }, tx, cancellationToken: ct));
        tx.Commit(); return priority;
    }

    public async Task<ExecutivePriorityDto> UpdateAsync(Guid o, Guid user, Guid id, UpdatePriorityRequest r, CancellationToken ct) {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(); await ValidateReferences(c, tx, o, user, r.OwnerUserId, r.SourceType, r.SourceId);
        var sql = $"UPDATE valorapesquisa.executive_priorities p SET title=@Title,description=@Description,priority=@Priority,owner_user_id=@OwnerUserId,due_at=@DueAt,source_type=@SourceType,source_id=@SourceId,updated_at=now() WHERE p.organization_id=@o AND p.id=@id AND p.status='active' AND p.updated_at=@ExpectedUpdatedAt RETURNING {Projection.Replace("u.name OwnerName", "NULL::text OwnerName")}";
        var result = await c.QuerySingleOrDefaultAsync<ExecutivePriorityDto>(new CommandDefinition(sql, new { o, id, Title=r.Title.Trim(), Description=Clean(r.Description), r.Priority, r.OwnerUserId, r.DueAt, SourceType=Clean(r.SourceType), r.SourceId, r.ExpectedUpdatedAt }, tx, cancellationToken: ct));
        if (result is null) throw await Failure(c, tx, o, id, r.ExpectedUpdatedAt, ct);
        await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.workspace_items SET title=@Title,summary=@Description,priority=@Priority,due_at=@DueAt,owner_user_id=@OwnerUserId,source_type=@SourceType,source_id=@SourceId WHERE organization_id=@o AND id=@id; INSERT INTO valorapesquisa.executive_priority_updates(organization_id,priority_id,progress_percent,note,event_type,created_by) VALUES(@o,@id,@progress,'Prioridade editada','edited',@user)", new { o,id,user,Title=r.Title.Trim(),Description=Clean(r.Description),r.Priority,r.OwnerUserId,r.DueAt,SourceType=Clean(r.SourceType),r.SourceId,progress=result.ProgressPercent }, tx, cancellationToken:ct)); tx.Commit(); return result;
    }

    public Task<PriorityDetailsDto> ProgressAsync(Guid o, Guid user, Guid id, ProgressPriorityRequest r, bool wide, bool canManage, CancellationToken ct) => Mutate(o,user,id,"progress",r.Note,r.CommandId,r.ExpectedUpdatedAt,r.ProgressPercent,wide,canManage,ct);
    public Task<PriorityDetailsDto> TransitionAsync(Guid o, Guid user, Guid id, string command, TransitionPriorityRequest r, bool wide, bool canManage, CancellationToken ct) {
        if (command is not ("complete" or "cancel" or "reopen")) throw new ValidationAppException("Transição inválida.");
        return Mutate(o,user,id,command,r.Justification,r.CommandId,r.ExpectedUpdatedAt,null,wide,canManage,ct);
    }

    public async Task<PageResult<PriorityOptionDto>> OwnersAsync(Guid o,OptionQuery query,CancellationToken ct) {
        using var c=connections.Create(); var page=query.ValidPage; var size=query.ValidPageSize; var offset=(page-1)*size; var search=Clean(query.Search);
        const string where="organization_id=@o AND status='active' AND deleted_at IS NULL AND (@search IS NULL OR name ILIKE '%'||@search||'%')";
        var args=new{o,search,size,offset,include=query.IncludeId};
        var total=await c.ExecuteScalarAsync<int>(new CommandDefinition($"SELECT count(*) FROM valorapesquisa.users WHERE {where}",args,cancellationToken:ct));
        var rows=(await c.QueryAsync<PriorityOptionDto>(new CommandDefinition($"SELECT id Id,name Label FROM valorapesquisa.users WHERE ({where}) OR (organization_id=@o AND id=@include) ORDER BY (id=@include) DESC,name,id LIMIT @size OFFSET @offset",args,cancellationToken:ct))).AsList();
        return new(rows,page,size,total);
    }
    public async Task<PageResult<PrioritySourceOptionDto>> SourcesAsync(Guid o,Guid user,bool wide,OptionQuery query,CancellationToken ct) {
        using var c=connections.Create(); var page=query.ValidPage; var size=query.ValidPageSize; var offset=(page-1)*size; var search=Clean(query.Search);
        var where=$"organization_id=@o AND item_type<>'priority' AND {Visible.Replace("p.", "").Replace("@user", "@user")} AND (@search IS NULL OR title ILIKE '%'||@search||'%')";
        var args=new{o,user,wide,search,size,offset,include=query.IncludeId};
        var total=await c.ExecuteScalarAsync<int>(new CommandDefinition($"SELECT count(*) FROM valorapesquisa.workspace_items WHERE {where}",args,cancellationToken:ct));
        var rows=(await c.QueryAsync<PrioritySourceOptionDto>(new CommandDefinition($"SELECT id Id,item_type Type,title Label FROM valorapesquisa.workspace_items WHERE ({where}) OR (organization_id=@o AND id=@include AND item_type<>'priority' AND (@wide OR owner_user_id=@user OR owner_user_id IS NULL)) ORDER BY (id=@include) DESC,title,id LIMIT @size OFFSET @offset",args,cancellationToken:ct))).AsList();
        return new(rows,page,size,total);
    }

    private async Task<PriorityDetailsDto> Mutate(Guid o, Guid user, Guid id, string command, string note, string commandId, DateTimeOffset expected, int? requestedProgress, bool wide, bool canManage, CancellationToken ct) {
        using var c=connections.Create(); c.Open(); using var tx=c.BeginTransaction();
        var existing=await c.QuerySingleOrDefaultAsync<(string Status,int ProgressPercent,Guid? OwnerUserId,DateTimeOffset UpdatedAt)>(new CommandDefinition("SELECT status Status,progress_percent ProgressPercent,owner_user_id OwnerUserId,updated_at UpdatedAt FROM valorapesquisa.executive_priorities WHERE organization_id=@o AND id=@id FOR UPDATE",new{o,id},tx,cancellationToken:ct));
        if (existing == default) throw new KeyNotFoundException("Prioridade não encontrada.");
        if (!wide && existing.OwnerUserId is { } owner && owner != user) throw new UnauthorizedAccessException();
        if (!canManage) throw new UnauthorizedAccessException("Gerenciamento de prioridades não autorizado.");
        var fingerprint=Fingerprint(command,note,requestedProgress,expected);
        var duplicate=await c.QuerySingleOrDefaultAsync<(string Operation,string PayloadHash,Guid ActorId)>(new CommandDefinition("SELECT operation Operation,payload_hash PayloadHash,actor_id ActorId FROM valorapesquisa.executive_priority_updates WHERE organization_id=@o AND priority_id=@id AND command_id=@commandId",new{o,id,commandId},tx,cancellationToken:ct));
        if (duplicate != default) {
            if (duplicate.Operation!=command || duplicate.PayloadHash!=fingerprint || duplicate.ActorId!=user) throw new ConcurrencyConflictException("A chave de idempotência já foi usada para outro comando.");
            tx.Commit(); return (await GetAsync(o,user,id,wide,canManage,ct))!;
        }
        if (existing.UpdatedAt != expected) throw new ConcurrencyConflictException("Prioridade alterada por outra sessão.");
        var (status,progress)=command switch { "progress" when existing.Status=="active" => ("active",requestedProgress!.Value), "complete" when existing.Status=="active" => ("completed",100), "cancel" when existing.Status=="active" => ("cancelled",existing.ProgressPercent), "reopen" when existing.Status is "completed" or "cancelled" => ("active",existing.ProgressPercent), _ => throw new BusinessRuleAppException("Transição incompatível com o estado atual.") };
        await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.executive_priorities SET status=@status,progress_percent=@progress,updated_at=now() WHERE organization_id=@o AND id=@id; UPDATE valorapesquisa.workspace_items SET status=@status,summary=(SELECT description FROM valorapesquisa.executive_priorities WHERE id=@id),owner_user_id=(SELECT owner_user_id FROM valorapesquisa.executive_priorities WHERE id=@id),updated_at=now(),completed_at=CASE WHEN @status='completed' THEN now() ELSE NULL END WHERE organization_id=@o AND id=@id AND item_type='priority'; INSERT INTO valorapesquisa.executive_priority_updates(organization_id,priority_id,progress_percent,note,event_type,command_id,operation,payload_hash,actor_id,created_by) VALUES(@o,@id,@progress,@note,@command,@commandId,@command,@fingerprint,@user,@user)",new{o,id,user,status,progress,note=note.Trim(),command,commandId,fingerprint},tx,cancellationToken:ct));
        var projectionValid=await c.ExecuteScalarAsync<bool>(new CommandDefinition("SELECT EXISTS(SELECT 1 FROM valorapesquisa.workspace_items WHERE organization_id=@o AND id=@id AND item_type='priority' AND status=@status AND owner_user_id IS NOT DISTINCT FROM (SELECT owner_user_id FROM valorapesquisa.executive_priorities WHERE organization_id=@o AND id=@id))",new{o,id,status},tx,cancellationToken:ct));
        if(!projectionValid) throw new BusinessRuleAppException("A projeção da prioridade no Workspace está ausente ou inconsistente.");
        tx.Commit();
        return (await GetAsync(o,user,id,wide,canManage,ct))!;
    }

    private static string Fingerprint(string operation,string note,int? progress,DateTimeOffset expected) {
        var value=$"{operation}\n{note.Trim()}\n{progress?.ToString() ?? "-"}\n{expected:O}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
    private static PriorityDetailsDto Details(ExecutivePriorityDto p,IReadOnlyList<PriorityUpdateDto> h,bool manage) => new(p,h,new(manage&&p.Status=="active",manage&&p.Status=="active",manage&&p.Status=="active",manage&&p.Status=="active",manage&&p.Status=="active",manage&&p.Status is "completed" or "cancelled"));
    private static string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
    private static async Task ValidateReferences(System.Data.IDbConnection c,System.Data.IDbTransaction tx,Guid o,Guid user,Guid? owner,string? sourceType,Guid? sourceId) {
        if(owner.HasValue&&!await c.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM valorapesquisa.users WHERE id=@owner AND organization_id=@o AND status='active' AND deleted_at IS NULL)",new{o,owner},tx)) throw new ValidationAppException("Responsável inválido para este cliente.");
        if(sourceId.HasValue != !string.IsNullOrWhiteSpace(sourceType)) throw new ValidationAppException("Informe tipo e recurso de origem em conjunto.");
        if(sourceId.HasValue&&!await c.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM valorapesquisa.workspace_items WHERE organization_id=@o AND ((id=@sourceId AND item_type=@sourceType) OR (source_id=@sourceId AND source_type=@sourceType)) AND (owner_user_id IS NULL OR owner_user_id=@user))",new{o,user,sourceId,sourceType},tx)) throw new UnauthorizedAccessException("Origem indisponível.");
    }
    private static async Task<Exception> Failure(System.Data.IDbConnection c,System.Data.IDbTransaction tx,Guid o,Guid id,DateTimeOffset expected,CancellationToken ct) { var row=await c.QuerySingleOrDefaultAsync<(string Status,DateTimeOffset UpdatedAt)>(new CommandDefinition("SELECT status Status,updated_at UpdatedAt FROM valorapesquisa.executive_priorities WHERE organization_id=@o AND id=@id",new{o,id},tx,cancellationToken:ct)); return row==default?new KeyNotFoundException():row.Status!="active"?new BusinessRuleAppException("Prioridade encerrada não aceita edição."):new ConcurrencyConflictException("Prioridade alterada por outra sessão."); }
}
