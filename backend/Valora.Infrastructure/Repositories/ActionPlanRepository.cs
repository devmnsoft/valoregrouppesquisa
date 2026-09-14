using Dapper;
using System.Data;
using Valora.Application.ActionCenter;
using Valora.Application.Contracts;
using Valora.Application.Workspace;
namespace Valora.Infrastructure.Repositories;

public sealed class ActionPlanRepository(IDbConnectionFactory db) : IActionPlanRepository {
    const string Select = "SELECT p.id,p.title,p.summary,p.origin_type OriginType,p.status,p.priority,p.owner_user_id OwnerUserId,p.due_at DueAt,p.evidence_summary EvidenceSummary,p.expected_outcome ExpectedOutcome,coalesce(round(avg(i.progress_percent))::int,0) ProgressPercent,p.created_at CreatedAt FROM valorapesquisa.action_plans p LEFT JOIN valorapesquisa.action_items i ON i.action_plan_id=p.id AND i.deleted_at IS NULL AND i.status<>'canceled'";
    public async Task<IReadOnlyList<ActionPlanDto>> List(Guid o, CancellationToken c) { using var x = db.Create(); return (await x.QueryAsync<ActionPlanDto>(new CommandDefinition(Select + " WHERE p.organization_id=@o AND p.deleted_at IS NULL GROUP BY p.id ORDER BY p.created_at DESC", new { o }, cancellationToken: c))).ToList(); }
    public async Task<PageResult<ActionOptionDto>> Options(Guid o, Guid u, bool wide, OptionQuery query, CancellationToken c) {
        var page=query.ValidPage; var size=query.ValidPageSize; var offset=checked((page-1)*size); var args=OptionParameters(o,u,wide,query.Search,offset,size);
        const string scope="p.organization_id=@o AND p.deleted_at IS NULL AND p.status=ANY(@statuses) AND (@wide OR p.owner_user_id IS NULL OR p.owner_user_id=@u) AND (@search IS NULL OR p.title ILIKE '%'||@search||'%')";
        var sql=$"""
            SELECT count(*)::int FROM valorapesquisa.action_plans p WHERE {scope};
            SELECT p.id Id,p.title Title,p.status Status,p.priority Priority,p.summary Context,p.owner_user_id ResponsibleUserId,u2.name ResponsibleName
            FROM valorapesquisa.action_plans p LEFT JOIN valorapesquisa.users u2 ON u2.id=p.owner_user_id AND u2.organization_id=p.organization_id AND u2.deleted_at IS NULL
            WHERE {scope} ORDER BY p.title,p.id LIMIT @size OFFSET @offset;
            """;
        using var x=db.Create(); using var result=await x.QueryMultipleAsync(new CommandDefinition(sql,args,cancellationToken:c));
        var total=await result.ReadSingleAsync<int>(); var rows=(await result.ReadAsync<ActionOptionDto>()).AsList(); return new(rows,page,size,total);
    }
    public async Task<ActionPlanDto?> Get(Guid o, Guid id, CancellationToken c) { using var x = db.Create(); return await x.QuerySingleOrDefaultAsync<ActionPlanDto>(new CommandDefinition(Select + " WHERE p.organization_id=@o AND p.id=@id AND p.deleted_at IS NULL GROUP BY p.id", new { o, id }, cancellationToken: c)); }
    public async Task<ActionDashboardDto> Dashboard(Guid o, CancellationToken c) {
        var plans = await List(o, c); using var x = db.Create();
        const string sql = """
            SELECT count(*) FILTER(WHERE priority='critical' AND status NOT IN('completed','canceled'))::int Critical,
                   count(*) FILTER(WHERE due_at<now() AND status NOT IN('completed','canceled'))::int Overdue,
                   count(*) FILTER(WHERE responsible_user_id IS NULL AND status NOT IN('completed','canceled'))::int Unassigned,
                   count(*) FILTER(WHERE status='blocked')::int Blocked
            FROM valorapesquisa.action_items WHERE organization_id=@o AND deleted_at IS NULL;
            SELECT id,action_plan_id ActionPlanId,title,description,origin_type OriginType,origin_id OriginId,related_dimension RelatedDimension,related_index_code RelatedIndexCode,priority,status,responsible_user_id ResponsibleUserId,due_at DueAt,completed_at CompletedAt,progress_percent ProgressPercent,evidence_summary EvidenceSummary,expected_outcome ExpectedOutcome,completion_evidence CompletionEvidence,ai_recommendation_summary AiRecommendationSummary,created_at CreatedAt,version Version
            FROM valorapesquisa.action_items WHERE organization_id=@o AND deleted_at IS NULL AND status NOT IN('completed','canceled') ORDER BY due_at NULLS LAST,created_at DESC LIMIT 12;
            """;
        using var q=await x.QueryMultipleAsync(new CommandDefinition(sql,new{o},cancellationToken:c)); var totals=await q.ReadSingleAsync<DashboardTotals>(); var items=(await q.ReadAsync<ActionItemDto>()).AsList();
        return new(totals.Critical,totals.Overdue,totals.Unassigned,plans.Count(p=>p.Status is "approved" or "in_execution"),totals.Blocked,items,plans.Take(6).ToList());
    }
    private sealed record DashboardTotals(int Critical,int Overdue,int Unassigned,int Blocked);
    public async Task<Guid> Create(Guid o, Guid u, CreateActionPlanRequest r, CancellationToken c) { var id = Guid.NewGuid(); using var x = db.Create(); await x.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.action_plans(id,organization_id,diagnostic_id,result_id,governance_cycle_id,title,summary,origin_type,origin_id,status,priority,owner_user_id,created_by_user_id,starts_at,due_at,evidence_summary,expected_outcome) VALUES(@id,@o,@DiagnosticId,@ResultId,@GovernanceCycleId,@Title,@Summary,@OriginType,@OriginId,'draft',@Priority,@OwnerUserId,@u,@StartsAt,@DueAt,@EvidenceSummary,@ExpectedOutcome)", new { id, o, u, r.DiagnosticId, r.ResultId, r.GovernanceCycleId, r.Title, r.Summary, r.OriginType, r.OriginId, r.Priority, r.OwnerUserId, r.StartsAt, r.DueAt, r.EvidenceSummary, r.ExpectedOutcome }, cancellationToken: c)); return id; }
    private static DynamicParameters OptionParameters(Guid o,Guid u,bool wide,string? search,int offset,int size) { var p=new DynamicParameters();p.Add("o",o,DbType.Guid);p.Add("u",u,DbType.Guid);p.Add("wide",wide,DbType.Boolean);p.Add("search",string.IsNullOrWhiteSpace(search)?null:search.Trim(),DbType.String);p.Add("statuses",new[]{"draft","proposed","approved","in_execution"});p.Add("offset",offset,DbType.Int32);p.Add("size",size,DbType.Int32);return p; }
}
