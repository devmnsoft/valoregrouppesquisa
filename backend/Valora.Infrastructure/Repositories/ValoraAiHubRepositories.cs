using System.Text.Json;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.ActionCenter;
using Valora.Application.Workspace;
using Valora.Application.ValoraAi;

namespace Valora.Infrastructure.Repositories;

public sealed class ValoraAiEvidenceRepository(IDbConnectionFactory connections) : IValoraAiEvidenceRepository {
    public async Task<AiEvidencePack> BuildAsync(AiRunContext context, CancellationToken ct) {
        using var db = connections.Create();
        const string sql = """
            SELECT id, 'result' AS Type, 'results' AS SourceType, id AS SourceId,
                   'Resultado diagnóstico consolidado disponível' AS Summary,
                   NULL::text AS Dimension, NULL::text AS IndexCode, true AS IsAggregate
            FROM valorapesquisa.results
            WHERE organization_id=@OrganizationId AND id=@ResultId
            """;
        var items = context.ResultId is null ? [] : (await db.QueryAsync<AiEvidenceItem>(new CommandDefinition(sql,
            new { context.OrganizationId, context.ResultId }, cancellationToken: ct))).ToArray();
        var limitations = items.Length == 0 ? [AiInsufficientEvidence.Message] : Array.Empty<string>();
        return new AiEvidencePack(Guid.NewGuid(), context, items, limitations, DateTime.UtcNow);
    }

    public async Task SaveAsync(AiEvidencePack pack, Guid aiRunId, CancellationToken ct) {
        using var db = connections.Create();
        const string packSql = """
            INSERT INTO valorapesquisa.valora_ai_evidence_packs
              (id,organization_id,diagnostic_id,result_id,methodology_version_id,ai_run_id,evidence_count,limitation)
            VALUES (@Id,@OrganizationId,@DiagnosticId,@ResultId,@MethodologyVersionId,@AiRunId,@EvidenceCount,@Limitation)
            """;
        await db.ExecuteAsync(new CommandDefinition(packSql, new {
            pack.Id,
            pack.Context.OrganizationId,
            pack.Context.DiagnosticId,
            pack.Context.ResultId,
            pack.Context.MethodologyVersionId,
            AiRunId = aiRunId,
            EvidenceCount = pack.Items.Count,
            Limitation = string.Join(' ', pack.Limitations)
        }, cancellationToken: ct));
        const string itemSql = """
            INSERT INTO valorapesquisa.valora_ai_evidence_items
              (id,organization_id,evidence_pack_id,evidence_type,source_type,source_id,summary,related_dimension,related_index_code,is_aggregate)
            VALUES (@Id,@OrganizationId,@PackId,@Type,@SourceType,@SourceId,@Summary,@Dimension,@IndexCode,@IsAggregate)
            """;
        foreach (var item in pack.Items)
            await db.ExecuteAsync(new CommandDefinition(itemSql, new {
                item.Id,
                pack.Context.OrganizationId,
                PackId = pack.Id,
                item.Type,
                item.SourceType,
                item.SourceId,
                item.Summary,
                item.Dimension,
                item.IndexCode,
                item.IsAggregate
            }, cancellationToken: ct));
    }
}

public sealed class ValoraAiInsightRepository(IDbConnectionFactory connections) : IValoraAiInsightRepository {
    private const string Projection = "vi.id,vi.organization_id OrganizationId,vi.diagnostic_id DiagnosticId,vi.result_id ResultId,vi.ai_run_id AiRunId,vi.insight_type InsightType,vi.title,vi.summary,vi.evidence_summary EvidenceSummary,vi.related_dimension RelatedDimension,vi.related_index_code RelatedIndexCode,vi.severity,vi.priority,vi.confidence_level ConfidenceLevel,vi.limitation,vi.recommendation,vi.status,vi.created_at CreatedAt,vi.updated_at UpdatedAt,vi.review_version ReviewVersion,vi.reviewed_by_user_id ReviewedByUserId,vi.reviewed_at ReviewedAt,(SELECT ap.id FROM valorapesquisa.action_plans ap WHERE ap.organization_id=vi.organization_id AND ap.origin_type='ai_insight' AND ap.origin_id=vi.id AND ap.deleted_at IS NULL ORDER BY ap.created_at,ap.id LIMIT 1) LinkedPlanId";
    public async Task<AiInsight?> GetAsync(Guid organizationId, Guid id, CancellationToken ct) { using var db = connections.Create(); return await db.QuerySingleOrDefaultAsync<AiInsight>(new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.valora_ai_insights vi WHERE vi.organization_id=@organizationId AND vi.id=@id AND vi.deleted_at IS NULL", new { organizationId, id }, cancellationToken: ct)); }
    public async Task<IReadOnlyList<AiInsight>> ListAsync(Guid organizationId, string? status, CancellationToken ct) { using var db = connections.Create(); return (await db.QueryAsync<AiInsight>(new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.valora_ai_insights vi WHERE vi.organization_id=@organizationId AND vi.deleted_at IS NULL AND (@status IS NULL OR vi.status=@status) ORDER BY vi.created_at DESC", new { organizationId, status }, cancellationToken: ct))).ToArray(); }
    public async Task<AiInsightListResult> ListAsync(Guid organizationId, Guid userId, bool organizationWide, AiInsightListQuery query, CancellationToken ct) {
        var page=query.ValidPage;var size=query.ValidPageSize;var offset=(page-1)*size;
        var search=string.IsNullOrWhiteSpace(query.Search)?null:query.Search.Trim();
        var status=new[]{AiInsightStatuses.PendingReview,AiInsightStatuses.Approved,AiInsightStatuses.Rejected,AiInsightStatuses.ConvertedToAction,AiInsightStatuses.Archived}.Contains(query.Status)?query.Status:null;
        var priority=new[]{"critical","high","medium","low"}.Contains(query.Priority)?query.Priority:null;
        var dimension=string.IsNullOrWhiteSpace(query.Dimension)?null:query.Dimension.Trim();
        var order=query.Sort switch{"title"=>"vi.title,vi.id","priority"=>"CASE vi.priority WHEN 'critical' THEN 1 WHEN 'high' THEN 2 WHEN 'medium' THEN 3 WHEN 'low' THEN 4 ELSE 5 END,vi.created_at DESC,vi.id DESC",_=>"vi.created_at DESC,vi.id DESC"};
        const string filter="vi.organization_id=@organizationId AND vi.deleted_at IS NULL AND (@search IS NULL OR vi.title ILIKE '%'||@search||'%' OR vi.summary ILIKE '%'||@search||'%') AND (@status IS NULL OR vi.status=@status) AND (@priority IS NULL OR vi.priority=@priority) AND (@dimension IS NULL OR vi.related_dimension=@dimension) AND (@diagnosticId IS NULL OR vi.diagnostic_id=@diagnosticId) AND (@hasPlan IS NULL OR @hasPlan=EXISTS(SELECT 1 FROM valorapesquisa.action_plans hp WHERE hp.organization_id=vi.organization_id AND hp.origin_type='ai_insight' AND hp.origin_id=vi.id AND hp.deleted_at IS NULL AND (@organizationWide OR hp.owner_user_id IS NULL OR hp.owner_user_id=@userId OR EXISTS(SELECT 1 FROM valorapesquisa.action_items hai WHERE hai.action_plan_id=hp.id AND hai.organization_id=hp.organization_id AND hai.deleted_at IS NULL AND hai.responsible_user_id=@userId))))";
        var scope="(@organizationWide OR p.owner_user_id IS NULL OR p.owner_user_id=@userId OR EXISTS(SELECT 1 FROM valorapesquisa.action_items ai WHERE ai.action_plan_id=p.id AND ai.organization_id=p.organization_id AND ai.deleted_at IS NULL AND ai.responsible_user_id=@userId))";
        var args=new{organizationId,userId,organizationWide,search,status,priority,dimension,diagnosticId=query.DiagnosticId,hasPlan=query.HasPlan,size,offset};
        var sql=$"""
            SELECT count(*)::int FROM valorapesquisa.valora_ai_insights vi WHERE {filter};
            SELECT {Projection},d.title DiagnosticName,
              count(p.id)::int PlanCount,
              count(p.id) FILTER(WHERE p.status IN('approved','in_execution'))::int ActivePlanCount,
              count(p.id) FILTER(WHERE p.status IN('completed','canceled'))::int ClosedPlanCount
            FROM valorapesquisa.valora_ai_insights vi
            LEFT JOIN valorapesquisa.diagnostics d ON d.id=vi.diagnostic_id AND d.organization_id=vi.organization_id AND d.deleted_at IS NULL
            LEFT JOIN valorapesquisa.action_plans p ON p.organization_id=vi.organization_id AND p.origin_type='ai_insight' AND p.origin_id=vi.id AND p.deleted_at IS NULL AND {scope}
            WHERE {filter} GROUP BY vi.id,d.title ORDER BY {order} LIMIT @size OFFSET @offset;
            SELECT
              count(*) FILTER(WHERE vi.status='pending_review')::int PendingReview,
              count(*) FILTER(WHERE vi.status='approved' AND NOT EXISTS(SELECT 1 FROM valorapesquisa.action_plans p WHERE p.organization_id=vi.organization_id AND p.origin_type='ai_insight' AND p.origin_id=vi.id AND p.deleted_at IS NULL AND {scope}))::int ApprovedWithoutPlan,
              count(*) FILTER(WHERE EXISTS(SELECT 1 FROM valorapesquisa.action_plans p WHERE p.organization_id=vi.organization_id AND p.origin_type='ai_insight' AND p.origin_id=vi.id AND p.deleted_at IS NULL AND p.status='in_execution' AND {scope}))::int WithPlansInExecution,
              count(*) FILTER(WHERE EXISTS(SELECT 1 FROM valorapesquisa.action_plans p WHERE p.organization_id=vi.organization_id AND p.origin_type='ai_insight' AND p.origin_id=vi.id AND p.deleted_at IS NULL AND {scope}) AND NOT EXISTS(SELECT 1 FROM valorapesquisa.action_plans p WHERE p.organization_id=vi.organization_id AND p.origin_type='ai_insight' AND p.origin_id=vi.id AND p.deleted_at IS NULL AND p.status NOT IN('completed','canceled') AND {scope}))::int WithAllPlansClosed
            FROM valorapesquisa.valora_ai_insights vi WHERE vi.organization_id=@organizationId AND vi.deleted_at IS NULL;
            SELECT count(*)::int FROM valorapesquisa.valora_ai_insights vi WHERE vi.organization_id=@organizationId AND vi.deleted_at IS NULL;
            SELECT d.id,coalesce(nullif(trim(d.title),''),'Diagnóstico sem nome') Name FROM valorapesquisa.diagnostics d WHERE d.organization_id=@organizationId AND d.deleted_at IS NULL AND EXISTS(SELECT 1 FROM valorapesquisa.valora_ai_insights vi WHERE vi.organization_id=d.organization_id AND vi.diagnostic_id=d.id AND vi.deleted_at IS NULL) ORDER BY Name,d.id;
            """;
        using var db=connections.Create();using var multi=await db.QueryMultipleAsync(new CommandDefinition(sql,args,cancellationToken:ct));
        var total=await multi.ReadSingleAsync<int>();var rows=(await multi.ReadAsync<AiInsight,QueueCounts,AiInsightListItem>((i,x)=>new(i,x.DiagnosticName,x.PlanCount,x.ActivePlanCount,x.ClosedPlanCount),splitOn:"DiagnosticName")).AsList();
        var indicators=await multi.ReadSingleAsync<AiInsightIndicators>();var authorizedTotal=await multi.ReadSingleAsync<int>();var diagnostics=(await multi.ReadAsync<AiDiagnosticOption>()).AsList();return new(new(rows,page,size,total),indicators,authorizedTotal,diagnostics);
    }
    public async Task<AiInsightDetails?> DetailsAsync(Guid organizationId, Guid userId, Guid id, bool organizationWide, CancellationToken ct) {
        var insight=await GetAsync(organizationId,id,ct);if(insight is null)return null;using var db=connections.Create();
        const string evidenceSql="""SELECT ei.id,ei.evidence_type Type,ei.source_type SourceType,ei.source_id SourceId,ei.summary,ei.related_dimension Dimension,ei.related_index_code IndexCode,ei.created_at CreatedAt,true IsAvailable,false IsRestricted FROM valorapesquisa.valora_ai_insight_evidence_links l JOIN valorapesquisa.valora_ai_evidence_items ei ON ei.id=l.evidence_item_id AND ei.organization_id=@organizationId AND ei.deleted_at IS NULL WHERE l.insight_id=@id ORDER BY ei.created_at,ei.id""";
        const string plansSql="""SELECT p.id,p.title,p.summary,p.origin_type OriginType,p.status,p.priority,p.owner_user_id OwnerUserId,p.due_at DueAt,p.evidence_summary EvidenceSummary,p.expected_outcome ExpectedOutcome,coalesce(round(avg(i.progress_percent) FILTER(WHERE i.status<>'canceled'))::int,0) ProgressPercent,p.created_at CreatedAt,u.name OwnerName,count(i.id) FILTER(WHERE i.status NOT IN('completed','canceled'))::int ActiveItemCount,p.version Version FROM valorapesquisa.action_plans p LEFT JOIN valorapesquisa.action_items i ON i.action_plan_id=p.id AND i.organization_id=p.organization_id AND i.deleted_at IS NULL LEFT JOIN valorapesquisa.users u ON u.id=p.owner_user_id AND u.organization_id=p.organization_id AND u.deleted_at IS NULL WHERE p.organization_id=@organizationId AND p.origin_type='ai_insight' AND p.origin_id=@id AND p.deleted_at IS NULL AND (@organizationWide OR p.owner_user_id IS NULL OR p.owner_user_id=@userId OR EXISTS(SELECT 1 FROM valorapesquisa.action_items ai WHERE ai.action_plan_id=p.id AND ai.organization_id=p.organization_id AND ai.deleted_at IS NULL AND ai.responsible_user_id=@userId)) GROUP BY p.id,u.name ORDER BY p.created_at,p.id""";
        using var multi=await db.QueryMultipleAsync(new CommandDefinition(evidenceSql+";"+plansSql,new{organizationId,userId,id,organizationWide},cancellationToken:ct));
        return new(insight,(await multi.ReadAsync<AiInsightEvidence>()).AsList(),(await multi.ReadAsync<ActionPlanDto>()).AsList());
    }
    public async Task<Guid> CreateAsync(AiRunContext c, Guid runId, AiInsightDraft x, CancellationToken ct) {
        using var db = connections.Create(); db.Open(); using var tx = db.BeginTransaction();
        var evidenceIds = x.EvidenceIds.Distinct().ToArray();
        var evidence = evidenceIds.Length == 0 ? [] : (await db.QueryAsync<EvidenceLinkProjection>(new CommandDefinition("""SELECT ei.id,ei.summary FROM valorapesquisa.valora_ai_evidence_items ei JOIN valorapesquisa.valora_ai_evidence_packs ep ON ep.id=ei.evidence_pack_id AND ep.organization_id=ei.organization_id WHERE ei.organization_id=@OrganizationId AND ep.ai_run_id=@runId AND ei.deleted_at IS NULL AND ei.id=ANY(@evidenceIds) ORDER BY ei.created_at,ei.id""",new{c.OrganizationId,runId,evidenceIds},tx,cancellationToken:ct))).ToArray();
        if(evidence.Length!=evidenceIds.Length) throw new ArgumentException("Uma ou mais evidências não pertencem à organização ou à execução informada.",nameof(x));
        var id=Guid.NewGuid();
        const string sql="""INSERT INTO valorapesquisa.valora_ai_insights(id,organization_id,diagnostic_id,result_id,ai_run_id,insight_type,title,summary,evidence_summary,related_dimension,related_index_code,severity,priority,confidence_level,limitation,recommendation,status) VALUES(@id,@OrganizationId,@DiagnosticId,@ResultId,@runId,@InsightType,@Title,@Summary,@EvidenceSummary,@RelatedDimension,@RelatedIndexCode,@Severity,@Priority,@ConfidenceLevel,@Limitation,@Recommendation,'pending_review')""";
        await db.ExecuteAsync(new CommandDefinition(sql,new{id,c.OrganizationId,c.DiagnosticId,c.ResultId,runId,x.InsightType,x.Title,x.Summary,EvidenceSummary=string.Join("; ",evidence.Select(e=>e.Summary)),x.RelatedDimension,x.RelatedIndexCode,x.Severity,x.Priority,x.ConfidenceLevel,x.Limitation,x.Recommendation},tx,cancellationToken:ct));
        if(evidence.Length>0) await db.ExecuteAsync(new CommandDefinition("""INSERT INTO valorapesquisa.valora_ai_insight_evidence_links(insight_id,evidence_item_id,link_source) SELECT @id,unnest(@evidenceIds::uuid[]),'generation' ON CONFLICT(insight_id,evidence_item_id) DO NOTHING""",new{id,evidenceIds},tx,cancellationToken:ct));
        tx.Commit(); return id;
    }
    private sealed record QueueCounts(string? DiagnosticName,int PlanCount,int ActivePlanCount,int ClosedPlanCount);
    private sealed record EvidenceLinkProjection(Guid Id,string Summary);
}

public sealed class ValoraAiReviewRepository(IDbConnectionFactory connections) : IValoraAiReviewRepository {
    public async Task<AiReviewResult> ApplyAsync(AiReviewCommand c, CancellationToken ct) {
        using var db = connections.Create(); db.Open(); using var tx = db.BeginTransaction();
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{c.OrganizationId:N}|{c.InsightId:N}|{c.ReviewerId:N}|{c.ExpectedVersion}|{c.Decision}|{c.Reason?.Trim()}"))).ToLowerInvariant();
        await db.ExecuteAsync(new CommandDefinition("SELECT pg_advisory_xact_lock(hashtextextended(@key,0))", new { key = $"{c.OrganizationId:N}:insight-review:{c.CommandKey}" }, tx, cancellationToken: ct));
        var validReviewer = await db.ExecuteScalarAsync<bool>(new CommandDefinition("SELECT EXISTS(SELECT 1 FROM valorapesquisa.users WHERE id=@ReviewerId AND organization_id=@OrganizationId AND status='active' AND deleted_at IS NULL)", c, tx, cancellationToken: ct));
        if (!validReviewer) { tx.Rollback(); return AiReviewResult.NotFound; }
        var prior = await db.QuerySingleOrDefaultAsync<string>(new CommandDefinition("SELECT command_hash FROM valorapesquisa.valora_ai_review_commands WHERE organization_id=@OrganizationId AND command_key=@CommandKey", c, tx, cancellationToken: ct));
        if (prior is not null) { tx.Commit(); return prior == hash ? AiReviewResult.Replayed : AiReviewResult.Conflict; }
        const string update = """UPDATE valorapesquisa.valora_ai_insights SET status=@Decision,reviewed_by_user_id=@ReviewerId,reviewed_at=now(),review_version=review_version+1,updated_at=now() WHERE organization_id=@OrganizationId AND id=@InsightId AND deleted_at IS NULL AND status='pending_review' AND review_version=@ExpectedVersion AND EXISTS(SELECT 1 FROM valorapesquisa.users reviewer WHERE reviewer.id=@ReviewerId AND reviewer.organization_id=@OrganizationId AND reviewer.status='active' AND reviewer.deleted_at IS NULL) RETURNING ai_run_id""";
        var runId = await db.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(update, c, tx, cancellationToken: ct));
        if (runId is null) { var exists = await db.ExecuteScalarAsync<bool>(new CommandDefinition("SELECT EXISTS(SELECT 1 FROM valorapesquisa.valora_ai_insights WHERE organization_id=@OrganizationId AND id=@InsightId AND deleted_at IS NULL)", c, tx, cancellationToken: ct)); tx.Rollback(); return exists ? AiReviewResult.Conflict : AiReviewResult.NotFound; }
        await db.ExecuteAsync(new CommandDefinition("""INSERT INTO valorapesquisa.valora_ai_review_queue(organization_id,insight_id,status,assigned_to_user_id,decision_reason,reviewed_at) VALUES(@OrganizationId,@InsightId,@Decision,@ReviewerId,@Reason,now()); INSERT INTO valorapesquisa.valora_ai_review_commands(organization_id,insight_id,command_key,command_hash,result_status,created_by_user_id) VALUES(@OrganizationId,@InsightId,@CommandKey,@hash,@Decision,@ReviewerId); INSERT INTO valorapesquisa.valora_ai_feedbacks(organization_id,insight_id,ai_run_id,feedback_type,reason,created_by_user_id) SELECT @OrganizationId,@InsightId,@runId,'rejection',@Reason,@ReviewerId WHERE @Decision='rejected'""", new { c.OrganizationId, c.InsightId, c.Decision, c.ReviewerId, Reason = c.Reason?.Trim(), c.CommandKey, hash, runId }, tx, cancellationToken: ct));
        tx.Commit(); return AiReviewResult.Applied;
    }
}
public sealed class ValoraAiFeedbackRepository(IDbConnectionFactory connections) : IValoraAiFeedbackRepository {
    public async Task RecordAsync(Guid o, Guid insightId, Guid runId, Guid userId, string type, string reason, CancellationToken ct) { using var db = connections.Create(); const string sql = """INSERT INTO valorapesquisa.valora_ai_feedbacks(organization_id,insight_id,ai_run_id,feedback_type,reason,created_by_user_id) VALUES(@o,@insightId,@runId,@type,@reason,@userId)"""; await db.ExecuteAsync(new CommandDefinition(sql, new { o, insightId, runId, userId, type, reason }, cancellationToken: ct)); }
}
