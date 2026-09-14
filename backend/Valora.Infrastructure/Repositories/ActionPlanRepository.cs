using Dapper;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Valora.Application.Exceptions;
using Valora.Application.ActionCenter;
using Valora.Application.Contracts;
using Valora.Application.Workspace;
namespace Valora.Infrastructure.Repositories;

public sealed class ActionPlanRepository(IDbConnectionFactory db, IDbTransactionFactory tx) : IActionPlanRepository {
    const string Projection = "SELECT p.id,p.title,p.summary,p.origin_type OriginType,p.status,p.priority,p.owner_user_id OwnerUserId,p.due_at DueAt,p.evidence_summary EvidenceSummary,p.expected_outcome ExpectedOutcome,coalesce(round(avg(i.progress_percent))::int,0) ProgressPercent,p.created_at CreatedAt,u.name OwnerName,count(i.id) FILTER(WHERE i.status NOT IN('completed','canceled'))::int ActiveItemCount FROM valorapesquisa.action_plans p LEFT JOIN valorapesquisa.action_items i ON i.action_plan_id=p.id AND i.organization_id=p.organization_id AND i.deleted_at IS NULL AND i.status<>'canceled' LEFT JOIN valorapesquisa.users u ON u.id=p.owner_user_id AND u.organization_id=p.organization_id AND u.deleted_at IS NULL";
    const string Scope = "p.organization_id=@o AND p.deleted_at IS NULL AND EXISTS(SELECT 1 FROM valorapesquisa.organization_modules om_scope WHERE om_scope.organization_id=@o AND om_scope.module_code='organizational_intelligence' AND om_scope.enabled) AND (@wide OR p.owner_user_id IS NULL OR p.owner_user_id=@u OR EXISTS(SELECT 1 FROM valorapesquisa.action_items ai WHERE ai.action_plan_id=p.id AND ai.organization_id=p.organization_id AND ai.deleted_at IS NULL AND ai.responsible_user_id=@u))";
    public async Task<IReadOnlyList<ActionPlanDto>> List(Guid o, Guid u, bool wide, CancellationToken c) { using var x=db.Create(); return (await x.QueryAsync<ActionPlanDto>(new CommandDefinition(Projection+$" WHERE {Scope} GROUP BY p.id,u.name ORDER BY p.created_at DESC",new{o,u,wide},cancellationToken:c))).AsList(); }
    public async Task<PageResult<ActionPlanDto>> List(Guid o,Guid u,bool wide,ActionPlanListQuery query,CancellationToken c) {
        var page=query.ValidPage;var size=query.ValidPageSize;var offset=checked((page-1)*size);
        var search=string.IsNullOrWhiteSpace(query.Search)?null:query.Search.Trim();
        var status=ActionStatuses.Plan.Contains(query.Status??"")?query.Status:null;
        var priority=ActionStatuses.Priorities.Contains(query.Priority??"")?query.Priority:null;
        var due=query.Due is "overdue" or "today" or "upcoming" or "none"?query.Due:null;
        var filtered=Scope+" AND (@search IS NULL OR p.title ILIKE '%'||@search||'%' OR p.summary ILIKE '%'||@search||'%') AND (@status IS NULL OR p.status=@status) AND (@active=false OR p.status=ANY(@activeStatuses)) AND (@owner IS NULL OR p.owner_user_id=@owner) AND (@priority IS NULL OR p.priority=@priority) AND (@due IS NULL OR @due='overdue' AND (p.due_at AT TIME ZONE (SELECT time_zone FROM valorapesquisa.organizations WHERE id=@o))::date < (now() AT TIME ZONE (SELECT time_zone FROM valorapesquisa.organizations WHERE id=@o))::date OR @due='today' AND (p.due_at AT TIME ZONE (SELECT time_zone FROM valorapesquisa.organizations WHERE id=@o))::date = (now() AT TIME ZONE (SELECT time_zone FROM valorapesquisa.organizations WHERE id=@o))::date OR @due='upcoming' AND (p.due_at AT TIME ZONE (SELECT time_zone FROM valorapesquisa.organizations WHERE id=@o))::date > (now() AT TIME ZONE (SELECT time_zone FROM valorapesquisa.organizations WHERE id=@o))::date OR @due='none' AND p.due_at IS NULL)";
        var order=query.Sort switch{"title"=>"p.title,p.id","due"=>"p.due_at NULLS LAST,p.id",_=>"p.created_at DESC,p.id DESC"};
        var args=new{o,u,wide,search,status,active=query.Scope==ActionPlanScopeFilter.Active,activeStatuses=new[]{"approved","in_execution"},owner=query.OwnerUserId,priority,due,size,offset};
        using var x=db.Create();using var q=await x.QueryMultipleAsync(new CommandDefinition($"SELECT count(*)::int FROM valorapesquisa.action_plans p WHERE {filtered}; {Projection} WHERE {filtered} GROUP BY p.id,u.name ORDER BY {order} LIMIT @size OFFSET @offset",args,cancellationToken:c));
        var total=await q.ReadSingleAsync<int>();var rows=(await q.ReadAsync<ActionPlanDto>()).AsList();return new(rows,page,size,total);
    }
    public async Task<PageResult<ActionOptionDto>> Options(Guid o, Guid u, bool wide, OptionQuery query, CancellationToken c) {
        var page=query.ValidPage;var size=query.ValidPageSize;var offset=checked((page-1)*size);var args=OptionParameters(o,u,wide,query.Search,offset,size);
        const string scope="p.organization_id=@o AND p.deleted_at IS NULL AND p.status=ANY(@statuses) AND (@wide OR p.owner_user_id IS NULL OR p.owner_user_id=@u) AND (@search IS NULL OR p.title ILIKE '%'||@search||'%')";
        var sql=$"SELECT count(*)::int FROM valorapesquisa.action_plans p WHERE {scope}; SELECT p.id Id,p.title Title,p.status Status,p.priority Priority,p.summary Context,p.owner_user_id ResponsibleUserId,u2.name ResponsibleName FROM valorapesquisa.action_plans p LEFT JOIN valorapesquisa.users u2 ON u2.id=p.owner_user_id AND u2.organization_id=p.organization_id AND u2.deleted_at IS NULL WHERE {scope} ORDER BY p.title,p.id LIMIT @size OFFSET @offset;";
        using var x=db.Create();using var result=await x.QueryMultipleAsync(new CommandDefinition(sql,args,cancellationToken:c));var total=await result.ReadSingleAsync<int>();return new((await result.ReadAsync<ActionOptionDto>()).AsList(),page,size,total);
    }
    public async Task<ActionPlanDto?> Get(Guid o, Guid u, Guid id, bool wide, CancellationToken c) { using var x=db.Create();return await x.QuerySingleOrDefaultAsync<ActionPlanDto>(new CommandDefinition(Projection+$" WHERE {Scope} AND p.id=@id GROUP BY p.id,u.name",new{o,u,id,wide},cancellationToken:c)); }
    public async Task<ActionDashboardDto> Dashboard(Guid o, Guid u, bool wide, CancellationToken c) {
        var plans=(await List(o,u,wide,new ActionPlanListQuery(PageSize:6),c)).Items;using var x=db.Create();
        const string sql="""
        SELECT count(*) FILTER(WHERE i.priority='critical' AND i.status NOT IN('completed','canceled'))::int Critical,count(*) FILTER(WHERE (i.due_at AT TIME ZONE org.time_zone)::date < (now() AT TIME ZONE org.time_zone)::date AND i.status NOT IN('completed','canceled'))::int Overdue,count(*) FILTER(WHERE i.responsible_user_id IS NULL AND i.status NOT IN('completed','canceled'))::int Unassigned,count(*) FILTER(WHERE i.status='blocked')::int Blocked,(SELECT count(*)::int FROM valorapesquisa.action_plans ap WHERE ap.organization_id=@o AND ap.deleted_at IS NULL AND ap.status IN('approved','in_execution') AND (@wide OR ap.owner_user_id IS NULL OR ap.owner_user_id=@u OR EXISTS(SELECT 1 FROM valorapesquisa.action_items ai WHERE ai.action_plan_id=ap.id AND ai.organization_id=ap.organization_id AND ai.deleted_at IS NULL AND ai.responsible_user_id=@u))) ActivePlans
        FROM valorapesquisa.action_items i JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id JOIN valorapesquisa.organizations org ON org.id=i.organization_id WHERE i.organization_id=@o AND i.deleted_at IS NULL AND p.deleted_at IS NULL AND EXISTS(SELECT 1 FROM valorapesquisa.organization_modules om_scope WHERE om_scope.organization_id=@o AND om_scope.module_code='organizational_intelligence' AND om_scope.enabled) AND (@wide OR i.responsible_user_id IS NULL OR i.responsible_user_id=@u OR p.owner_user_id=@u);
        SELECT i.id,i.action_plan_id ActionPlanId,i.title,i.description,i.origin_type OriginType,i.origin_id OriginId,i.related_dimension RelatedDimension,i.related_index_code RelatedIndexCode,i.priority,i.status,i.responsible_user_id ResponsibleUserId,i.due_at DueAt,i.completed_at CompletedAt,i.progress_percent ProgressPercent,i.evidence_summary EvidenceSummary,i.expected_outcome ExpectedOutcome,i.completion_evidence CompletionEvidence,i.ai_recommendation_summary AiRecommendationSummary,i.created_at CreatedAt,i.version Version,ru.name ResponsibleName,p.title PlanTitle,i.metadata_json->>'completionResult' CompletionResult
        FROM valorapesquisa.action_items i JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id LEFT JOIN valorapesquisa.users ru ON ru.id=i.responsible_user_id AND ru.organization_id=i.organization_id AND ru.deleted_at IS NULL WHERE i.organization_id=@o AND i.deleted_at IS NULL AND p.deleted_at IS NULL AND i.status NOT IN('completed','canceled') AND (@wide OR i.responsible_user_id IS NULL OR i.responsible_user_id=@u OR p.owner_user_id=@u) ORDER BY i.due_at NULLS LAST,i.created_at DESC,i.id LIMIT 12;
        """;
        using var q=await x.QueryMultipleAsync(new CommandDefinition(sql,new{o,u,wide},cancellationToken:c));var totals=await q.ReadSingleAsync<DashboardTotals>();var items=(await q.ReadAsync<ActionItemDto>()).AsList();return new(totals.Critical,totals.Overdue,totals.Unassigned,totals.ActivePlans,totals.Blocked,items,plans);
    }
    private sealed record DashboardTotals(int Critical,int Overdue,int Unassigned,int Blocked,int ActivePlans);
    public async Task<Guid> Create(Guid o,Guid u,CreateActionPlanRequest r,CancellationToken c){
        if(o==Guid.Empty||u==Guid.Empty)throw new UnauthorizedAccessException("Sessão ou organização inválida.");
        var normalized=new{Title=r.Title.Trim(),Summary=r.Summary.Trim(),r.OriginType,r.OriginId,r.DiagnosticId,r.ResultId,r.GovernanceCycleId,r.Priority,r.OwnerUserId,StartsAt=r.StartsAt?.Date,DueAt=r.DueAt?.Date,EvidenceSummary=r.EvidenceSummary.Trim(),ExpectedOutcome=r.ExpectedOutcome.Trim()};
        var command=r.CommandId.Trim();var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new{organization=o,actor=u,operation="create-action-plan",payload=normalized})))).ToLowerInvariant();
        await using var z=await tx.BeginAsync(c);
        await z.Connection.ExecuteAsync(new CommandDefinition("SELECT pg_advisory_xact_lock(hashtextextended(@lockKey,0))",new{lockKey=$"{o:N}:{u:N}:create-action-plan:{command}"},z.Transaction,cancellationToken:c));
        var replay=await z.Connection.QuerySingleOrDefaultAsync<CreateReplay>(new CommandDefinition("SELECT request_hash RequestHash,result_id ResultId FROM valorapesquisa.action_plan_create_commands WHERE organization_id=@o AND created_by_user_id=@u AND operation='create-action-plan' AND command_id=@command",new{o,u,command},z.Transaction,cancellationToken:c));
        if(replay is not null){if(replay.RequestHash!=fingerprint)throw new ConcurrencyConflictException("A chave de criação já foi usada com outro conteúdo. Revise os campos antes de tentar novamente.");await z.CommitAsync();return replay.ResultId;}
        var id=Guid.NewGuid();var affected=await z.Connection.ExecuteAsync(new CommandDefinition("""
        INSERT INTO valorapesquisa.action_plans(id,organization_id,diagnostic_id,result_id,governance_cycle_id,title,summary,origin_type,origin_id,status,priority,owner_user_id,created_by_user_id,starts_at,due_at,evidence_summary,expected_outcome)
        SELECT @id,@o,@DiagnosticId,@ResultId,@GovernanceCycleId,@Title,@Summary,@OriginType,@OriginId,'draft',@Priority,@OwnerUserId,@u,
          CASE WHEN @StartsAt IS NULL THEN NULL ELSE (@StartsAt::date::timestamp AT TIME ZONE org.time_zone) END,
          CASE WHEN @DueAt IS NULL THEN NULL ELSE ((@DueAt::date + 1)::timestamp AT TIME ZONE org.time_zone) - interval '1 microsecond' END,@EvidenceSummary,@ExpectedOutcome
        FROM valorapesquisa.organizations org WHERE org.id=@o AND org.deleted_at IS NULL
          AND EXISTS(SELECT 1 FROM valorapesquisa.users actor WHERE actor.id=@u AND actor.organization_id=@o AND actor.status='active' AND actor.deleted_at IS NULL)
          AND EXISTS(SELECT 1 FROM valorapesquisa.organization_modules om WHERE om.organization_id=@o AND om.module_code='organizational_intelligence' AND om.enabled)
          AND (@OwnerUserId IS NULL OR EXISTS(SELECT 1 FROM valorapesquisa.users usr WHERE usr.id=@OwnerUserId AND usr.organization_id=@o AND usr.status='active' AND usr.deleted_at IS NULL))
          AND (@DueAt IS NULL OR @DueAt::date >= (now() AT TIME ZONE org.time_zone)::date) AND (@StartsAt IS NULL OR @DueAt IS NULL OR @StartsAt::date<=@DueAt::date)
          AND (@DiagnosticId IS NULL OR EXISTS(SELECT 1 FROM valorapesquisa.diagnostics d WHERE d.id=@DiagnosticId AND d.organization_id=@o AND d.deleted_at IS NULL))
          AND (@ResultId IS NULL OR EXISTS(SELECT 1 FROM valorapesquisa.results rs WHERE rs.id=@ResultId AND rs.organization_id=@o))
          AND (@GovernanceCycleId IS NULL OR EXISTS(SELECT 1 FROM valorapesquisa.organizational_governance_cycles gc WHERE gc.id=@GovernanceCycleId AND gc.organization_id=@o AND gc.deleted_at IS NULL))
        """,new{id,o,u,normalized.Title,normalized.Summary,normalized.OriginType,normalized.OriginId,normalized.DiagnosticId,normalized.ResultId,normalized.GovernanceCycleId,normalized.Priority,normalized.OwnerUserId,normalized.StartsAt,normalized.DueAt,normalized.EvidenceSummary,normalized.ExpectedOutcome},z.Transaction,cancellationToken:c));
        if(affected!=1)throw new ConflictAppException("Não foi possível criar o plano. Confira organização, módulo contratado, responsável, referências e prazo.");
        await z.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.action_plan_create_commands(organization_id,created_by_user_id,command_id,operation,request_hash,result_id) VALUES(@o,@u,@command,'create-action-plan',@fingerprint,@id)",new{o,u,command,fingerprint,id},z.Transaction,cancellationToken:c));
        await z.CommitAsync();return id;
    }
    private sealed record CreateReplay(string RequestHash,Guid ResultId);
    private static DynamicParameters OptionParameters(Guid o,Guid u,bool wide,string? search,int offset,int size){var p=new DynamicParameters();p.Add("o",o,DbType.Guid);p.Add("u",u,DbType.Guid);p.Add("wide",wide);p.Add("search",string.IsNullOrWhiteSpace(search)?null:search.Trim());p.Add("statuses",new[]{"draft","proposed","approved","in_execution"});p.Add("offset",offset);p.Add("size",size);return p;}
}
