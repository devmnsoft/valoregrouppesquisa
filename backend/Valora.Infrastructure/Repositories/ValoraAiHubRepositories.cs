using System.Text.Json;
using Dapper;
using Valora.Application.Contracts;
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
    public async Task<Guid> CreateAsync(AiRunContext c, Guid runId, AiInsightDraft x, CancellationToken ct) { using var db = connections.Create(); var id = Guid.NewGuid(); const string sql = """INSERT INTO valorapesquisa.valora_ai_insights(id,organization_id,diagnostic_id,result_id,ai_run_id,insight_type,title,summary,evidence_summary,related_dimension,related_index_code,severity,priority,confidence_level,limitation,recommendation,status) VALUES(@id,@OrganizationId,@DiagnosticId,@ResultId,@runId,@InsightType,@Title,@Summary,@EvidenceSummary,@RelatedDimension,@RelatedIndexCode,@Severity,@Priority,@ConfidenceLevel,@Limitation,@Recommendation,'pending_review')"""; await db.ExecuteAsync(new CommandDefinition(sql, new { id, c.OrganizationId, c.DiagnosticId, c.ResultId, runId, x.InsightType, x.Title, x.Summary, EvidenceSummary = string.Join(", ", x.EvidenceIds), x.RelatedDimension, x.RelatedIndexCode, x.Severity, x.Priority, x.ConfidenceLevel, x.Limitation, x.Recommendation }, cancellationToken: ct)); return id; }
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
