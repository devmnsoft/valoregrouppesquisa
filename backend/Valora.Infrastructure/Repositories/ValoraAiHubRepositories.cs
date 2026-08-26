using System.Text.Json;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.ValoraAi;

namespace Valora.Infrastructure.Repositories;

public sealed class ValoraAiEvidenceRepository(IDbConnectionFactory connections) : IValoraAiEvidenceRepository
{
    public async Task<AiEvidencePack> BuildAsync(AiRunContext context, CancellationToken ct)
    {
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

    public async Task SaveAsync(AiEvidencePack pack, Guid aiRunId, CancellationToken ct)
    {
        using var db = connections.Create();
        const string packSql = """
            INSERT INTO valorapesquisa.valora_ai_evidence_packs
              (id,organization_id,diagnostic_id,result_id,methodology_version_id,ai_run_id,evidence_count,limitation)
            VALUES (@Id,@OrganizationId,@DiagnosticId,@ResultId,@MethodologyVersionId,@AiRunId,@EvidenceCount,@Limitation)
            """;
        await db.ExecuteAsync(new CommandDefinition(packSql, new { pack.Id, pack.Context.OrganizationId,
            pack.Context.DiagnosticId, pack.Context.ResultId, pack.Context.MethodologyVersionId, AiRunId = aiRunId,
            EvidenceCount = pack.Items.Count, Limitation = string.Join(' ', pack.Limitations) }, cancellationToken: ct));
        const string itemSql = """
            INSERT INTO valorapesquisa.valora_ai_evidence_items
              (id,organization_id,evidence_pack_id,evidence_type,source_type,source_id,summary,related_dimension,related_index_code,is_aggregate)
            VALUES (@Id,@OrganizationId,@PackId,@Type,@SourceType,@SourceId,@Summary,@Dimension,@IndexCode,@IsAggregate)
            """;
        foreach (var item in pack.Items)
            await db.ExecuteAsync(new CommandDefinition(itemSql, new { item.Id, pack.Context.OrganizationId,
                PackId = pack.Id, item.Type, item.SourceType, item.SourceId, item.Summary, item.Dimension,
                item.IndexCode, item.IsAggregate }, cancellationToken: ct));
    }
}

public sealed class ValoraAiInsightRepository(IDbConnectionFactory connections) : IValoraAiInsightRepository
{
    private const string Projection = "id,organization_id OrganizationId,diagnostic_id DiagnosticId,result_id ResultId,ai_run_id AiRunId,insight_type InsightType,title,summary,evidence_summary EvidenceSummary,related_dimension RelatedDimension,related_index_code RelatedIndexCode,severity,priority,confidence_level ConfidenceLevel,limitation,recommendation,status,created_at CreatedAt";
    public async Task<AiInsight?> GetAsync(Guid organizationId, Guid id, CancellationToken ct) { using var db=connections.Create(); return await db.QuerySingleOrDefaultAsync<AiInsight>(new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.valora_ai_insights WHERE organization_id=@organizationId AND id=@id AND deleted_at IS NULL",new{organizationId,id},cancellationToken:ct)); }
    public async Task<IReadOnlyList<AiInsight>> ListAsync(Guid organizationId,string? status,CancellationToken ct) { using var db=connections.Create(); return (await db.QueryAsync<AiInsight>(new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.valora_ai_insights WHERE organization_id=@organizationId AND deleted_at IS NULL AND (@status IS NULL OR status=@status) ORDER BY created_at DESC",new{organizationId,status},cancellationToken:ct))).ToArray(); }
    public async Task<Guid> CreateAsync(AiRunContext c,Guid runId,AiInsightDraft x,CancellationToken ct) { using var db=connections.Create(); var id=Guid.NewGuid(); const string sql="""INSERT INTO valorapesquisa.valora_ai_insights(id,organization_id,diagnostic_id,result_id,ai_run_id,insight_type,title,summary,evidence_summary,related_dimension,related_index_code,severity,priority,confidence_level,limitation,recommendation,status) VALUES(@id,@OrganizationId,@DiagnosticId,@ResultId,@runId,@InsightType,@Title,@Summary,@EvidenceSummary,@RelatedDimension,@RelatedIndexCode,@Severity,@Priority,@ConfidenceLevel,@Limitation,@Recommendation,'pending_review')"""; await db.ExecuteAsync(new CommandDefinition(sql,new{id,c.OrganizationId,c.DiagnosticId,c.ResultId,runId,x.InsightType,x.Title,x.Summary,EvidenceSummary=string.Join(", ",x.EvidenceIds),x.RelatedDimension,x.RelatedIndexCode,x.Severity,x.Priority,x.ConfidenceLevel,x.Limitation,x.Recommendation},cancellationToken:ct)); return id; }
    public async Task SetStatusAsync(Guid o,Guid id,string status,Guid userId,CancellationToken ct) { using var db=connections.Create(); await db.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.valora_ai_insights SET status=@status,reviewed_by_user_id=@userId,reviewed_at=now(),updated_at=now() WHERE organization_id=@o AND id=@id AND deleted_at IS NULL",new{o,id,status,userId},cancellationToken:ct)); }
}

public sealed class ValoraAiReviewRepository(IDbConnectionFactory connections) : IValoraAiReviewRepository
{
    public async Task RecordAsync(AiReviewCommand c,CancellationToken ct) { using var db=connections.Create(); const string sql="""INSERT INTO valorapesquisa.valora_ai_review_queue(organization_id,insight_id,status,assigned_to_user_id,reviewed_at) VALUES(@OrganizationId,@InsightId,@Decision,@ReviewerId,now())"""; await db.ExecuteAsync(new CommandDefinition(sql,c,cancellationToken:ct)); }
}
public sealed class ValoraAiFeedbackRepository(IDbConnectionFactory connections) : IValoraAiFeedbackRepository
{
    public async Task RecordAsync(Guid o,Guid insightId,Guid runId,Guid userId,string type,string reason,CancellationToken ct) { using var db=connections.Create(); const string sql="""INSERT INTO valorapesquisa.valora_ai_feedbacks(organization_id,insight_id,ai_run_id,feedback_type,reason,created_by_user_id) VALUES(@o,@insightId,@runId,@type,@reason,@userId)"""; await db.ExecuteAsync(new CommandDefinition(sql,new{o,insightId,runId,userId,type,reason},cancellationToken:ct)); }
}
