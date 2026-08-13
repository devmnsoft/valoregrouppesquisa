using System.Text.Json;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.OrganizationalIntelligence;

namespace Valora.Infrastructure.Repositories;

public sealed class IntelligencePipelineRepository(IDbConnectionFactory connections) : IIntelligencePipelineRepository
{
    public async Task<IReadOnlyList<Guid>> ExtractResponseEvidenceAsync(IntelligenceProcessingContext context, CancellationToken ct)
    {
        if (context.ResponseId is null) return await ExistingEvidence(context, ct);
        const string sql = """
            INSERT INTO valorapesquisa.evidence_items
              (organization_id,survey_id,response_id,form_id,question_id,concept_code,capability_code,dimension_code,
               metric_code,index_code,evidence_type,source_type,source_id,normalized_value,raw_value,weight,polarity,confidence_weight,text_excerpt,metadata_json)
            SELECT r.organization_id,r.survey_id,r.id,r.form_id,ra.question_id,qcm.concept_code,qcm.capability_code,qcm.dimension_code,
              qmm.metric_code,qim.index_code,qcm.evidence_type,'response',ra.id,
              CASE WHEN ra.max_score>0 THEN round((ra.score/ra.max_score*100)::numeric,4) END,
              coalesce(ra.answer_text,ra.answer_json::text),qcm.weight,qcm.polarity,
              CASE WHEN ra.score IS NULL THEN .60 ELSE 1 END,
              CASE WHEN ra.answer_text IS NULL THEN NULL ELSE left(ra.answer_text,500) END,
              jsonb_build_object('metricCode',qmm.metric_code,'indexCode',qim.index_code,'polarity',qcm.polarity)
            FROM valorapesquisa.responses r
            JOIN valorapesquisa.response_answers ra ON ra.response_id=r.id
            JOIN valorapesquisa.question_concept_mappings qcm ON qcm.question_id=ra.question_id AND qcm.deleted_at IS NULL
              AND (qcm.organization_id IS NULL OR qcm.organization_id=r.organization_id)
            LEFT JOIN valorapesquisa.question_metric_mappings qmm ON qmm.question_id=ra.question_id AND qmm.deleted_at IS NULL
              AND (qmm.organization_id IS NULL OR qmm.organization_id=r.organization_id)
            LEFT JOIN valorapesquisa.question_index_mappings qim ON qim.question_id=ra.question_id AND qim.deleted_at IS NULL
              AND (qim.organization_id IS NULL OR qim.organization_id=r.organization_id)
            WHERE r.id=@responseId AND r.organization_id=@organizationId
            ON CONFLICT(response_id,question_id,concept_code) WHERE deleted_at IS NULL DO UPDATE SET
              normalized_value=EXCLUDED.normalized_value,raw_value=EXCLUDED.raw_value,weight=EXCLUDED.weight,
              metric_code=EXCLUDED.metric_code,index_code=EXCLUDED.index_code,polarity=EXCLUDED.polarity,confidence_weight=EXCLUDED.confidence_weight,
              text_excerpt=EXCLUDED.text_excerpt,metadata_json=EXCLUDED.metadata_json
            RETURNING id
            """;
        using var db = connections.Create();
        return (await db.QueryAsync<Guid>(new CommandDefinition(sql, new { context.ResponseId, context.OrganizationId }, cancellationToken: ct))).ToList();
    }

    public async Task<ProcessingStageResult> CalculateMetricsAsync(IntelligenceProcessingContext c, IReadOnlyList<Guid> evidenceIds, CancellationToken ct)
    {
        const string sql = """
            WITH source AS (
              SELECT coalesce(metadata_json->>'metricCode',concept_code) code,
                sum(normalized_value*weight*confidence_weight)/nullif(sum(weight*confidence_weight),0) value,
                count(*)::int evidence_count,avg(confidence_weight) confidence
              FROM valorapesquisa.evidence_items WHERE organization_id=@organizationId AND id=ANY(@evidenceIds) AND normalized_value IS NOT NULL GROUP BY 1)
            INSERT INTO valorapesquisa.metric_values(organization_id,code,status,data,methodology_version,version)
            SELECT @organizationId,code,CASE WHEN evidence_count>=3 THEN 'calculated' ELSE 'insufficient_evidence' END,
              jsonb_build_object('value',round(value,2),'trend','baseline','confidence',round(confidence,2),'evidenceCount',evidence_count,
                'limitations',CASE WHEN evidence_count<3 THEN 'Dados insuficientes para interpretação isolada.' ELSE 'Interpretar em conjunto com índices e histórico.' END),1,1 FROM source
            RETURNING id
            """;
        return await ExecuteStage(c, evidenceIds, "metrics", sql, ct);
    }

    public async Task<ProcessingStageResult> CalculateIndicesAsync(IntelligenceProcessingContext c, IReadOnlyList<Guid> evidenceIds, CancellationToken ct)
    {
        const string sql = """
            WITH source AS (
              SELECT coalesce(metadata_json->>'indexCode','IMO') code,
               sum(normalized_value*weight*confidence_weight)/nullif(sum(weight*confidence_weight),0) score,
               count(*)::int evidence_count,avg(confidence_weight) confidence
              FROM valorapesquisa.evidence_items WHERE organization_id=@organizationId AND id=ANY(@evidenceIds) AND normalized_value IS NOT NULL GROUP BY 1)
            INSERT INTO valorapesquisa.index_values(organization_id,code,status,data,methodology_version,version)
            SELECT @organizationId,code,CASE WHEN evidence_count>=3 THEN 'calculated' ELSE 'insufficient_evidence' END,
              jsonb_build_object('score',round(score,2),'classification',CASE WHEN score<=25 THEN 'Inicial' WHEN score<=50 THEN 'Estruturante' WHEN score<=75 THEN 'Integrado' ELSE 'Maduro' END,
                'trend','baseline','confidence',round(confidence,2),'evidenceCount',evidence_count,'calculation','weighted_convergent_evidence'),1,1 FROM source RETURNING id
            """;
        return await ExecuteStage(c, evidenceIds, "indices", sql, ct);
    }

    public async Task<ProcessingStageResult> GenerateInferencesAsync(IntelligenceProcessingContext c, IReadOnlyList<Guid> evidenceIds, CancellationToken ct)
    {
        const string sql = """
            WITH source AS (SELECT concept_code,count(*)::int evidence_count,round(avg(normalized_value),2) score,array_agg(id) evidence
              FROM valorapesquisa.evidence_items WHERE organization_id=@organizationId AND id=ANY(@evidenceIds) GROUP BY concept_code),
            run AS (INSERT INTO valorapesquisa.inference_runs(organization_id,code,status,data) VALUES
              (@organizationId,@runCode,'completed',jsonb_build_object('source','evidence_pipeline','minimumEvidence',3)) RETURNING id)
            INSERT INTO valorapesquisa.inference_results(organization_id,code,status,data)
            SELECT @organizationId,concept_code,CASE WHEN evidence_count>=3 THEN 'moderate_confidence' ELSE 'insufficient_evidence' END,
              jsonb_build_object('runId',(SELECT id FROM run),'symptom','Padrão observado nas respostas agregadas','probableCause','Hipótese sistêmica a validar, não conclusão causal',
              'concept',concept_code,'evidenceIds',evidence,'evidenceCount',evidence_count,'score',score,
              'confidence',CASE WHEN evidence_count>=7 THEN 'very_high' WHEN evidence_count>=4 THEN 'high' WHEN evidence_count=3 THEN 'moderate' ELSE 'low' END,
              'limitation',CASE WHEN evidence_count<3 THEN 'Dados insuficientes para conclusão.' ELSE 'Inferência requer validação no contexto organizacional.' END,
              'rule','convergent-evidence-v1') FROM source RETURNING id
            """;
        return await ExecuteStage(c, evidenceIds, "inference", sql, ct);
    }

    public async Task<ProcessingStageResult> GenerateInsightsAsync(IntelligenceProcessingContext c, IReadOnlyList<Guid> evidenceIds, CancellationToken ct)
    {
        const string sql = """
            WITH valid AS (SELECT code,data FROM valorapesquisa.inference_results WHERE organization_id=@organizationId
              AND status<>'insufficient_evidence' AND data->'evidenceIds' ?| @evidenceTexts ORDER BY created_at DESC),
            run AS (INSERT INTO valorapesquisa.insight_runs(organization_id,code,status,data) VALUES
              (@organizationId,@runCode,'completed',jsonb_build_object('source','inference_engine','evidenceHash',md5(@evidenceHash))) RETURNING id)
            INSERT INTO valorapesquisa.insights(organization_id,code,status,data)
            SELECT @organizationId,code,'active',jsonb_build_object('runId',(SELECT id FROM run),'title','Prioridade sistêmica baseada em evidências: '||code,
              'executiveSummary','Inferência sustentada por evidências convergentes; validar o contexto antes de agir.',
              'type','systemic','priority','moderate','confidence',data->>'confidence','evidenceIds',data->'evidenceIds',
              'probableCause',data->>'probableCause','recommendation','Validar a causa provável e converter em ação mensurável.','validUntil',now()+interval '90 days')
            FROM valid RETURNING id
            """;
        return await ExecuteStage(c, evidenceIds, "insights", sql, ct);
    }

    public async Task<ProcessingStageResult> RefreshProjectionAsync(IntelligenceProcessingContext c, string module, IReadOnlyList<Guid> evidenceIds, CancellationToken ct)
    {
        var table = module switch { "action" => "action_items", "evolution" => "evolution_cycles", "heatmap" => "heatmap_snapshots", "radar" => "radar_snapshots", "benchmark" => "benchmark_runs", "executive_report" => "executive_reports", _ => throw new ArgumentOutOfRangeException(nameof(module)) };
        var status = evidenceIds.Count >= 3 ? "ready" : "insufficient_evidence";
        var data = JsonSerializer.Serialize(new { trigger = c.Trigger, evidenceIds, evidenceCount = evidenceIds.Count,
            limitation = evidenceIds.Count < 3 ? "Dados insuficientes para uma leitura confiável." : "Leitura agregada; não utilizar para avaliar pessoas." });
        using var db = connections.Create();
        var id = await db.QuerySingleAsync<Guid>(new CommandDefinition($"INSERT INTO valorapesquisa.{table}(organization_id,code,status,data) VALUES(@organizationId,@code,@status,CAST(@data AS jsonb)) RETURNING id", new { organizationId = c.OrganizationId, code = $"{module}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}", status, data }, cancellationToken: ct));
        return new(module, 1, evidenceIds.Count >= 3, status == "ready" ? "Snapshot histórico criado com evidências vinculadas." : "Snapshot preservado como dados insuficientes.", evidenceIds);
    }

    public async Task RecordEventAsync(IntelligenceProcessingContext c, Guid runId, string eventType, string title, string description, CancellationToken ct)
    {
        using var db = connections.Create();
        if (eventType.StartsWith("notification:"))
            await db.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.notifications(organization_id,user_id,type,title,body,message,related_module,related_entity_id) VALUES(@OrganizationId,@UserId,@type,@title,@description,@description,'organizational_intelligence',@runId)", new { c.OrganizationId, c.UserId, type = eventType[13..], title, description, runId }, cancellationToken: ct));
        else if (eventType.StartsWith("governance:"))
            await db.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.platform_governance_events(organization_id,code,status,data,created_by) VALUES(@OrganizationId,@type,'recorded',jsonb_build_object('description',@description,'runId',@runId),@UserId)", new { c.OrganizationId, c.UserId, type = eventType[11..], description, runId }, cancellationToken: ct));
        else
            await db.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.journey_events(organization_id,code,status,data,created_by) VALUES(@OrganizationId,@eventType,'recorded',jsonb_build_object('title',@title,'description',@description,'trigger',@Trigger,'runId',@runId,'confidentiality','organizational'),@UserId)", new { c.OrganizationId, c.UserId, c.Trigger, eventType, title, description, runId }, cancellationToken: ct));
    }

    private async Task<ProcessingStageResult> ExecuteStage(IntelligenceProcessingContext c, IReadOnlyList<Guid> evidenceIds, string stage, string sql, CancellationToken ct)
    {
        using var db = connections.Create();
        var ids = (await db.QueryAsync<Guid>(new CommandDefinition(sql, new { organizationId = c.OrganizationId, evidenceIds = evidenceIds.ToArray(), evidenceTexts = evidenceIds.Select(x => x.ToString()).ToArray(), evidenceHash = string.Join('|', evidenceIds), runCode = $"{stage}-{Guid.NewGuid():N}" }, cancellationToken: ct))).ToList();
        return new(stage, ids.Count, evidenceIds.Count >= 3, ids.Count == 0 ? "Nenhum resultado elegível foi produzido; a insuficiência foi preservada." : "Resultado calculado e versionado a partir de evidências rastreáveis.", evidenceIds);
    }
    private async Task<IReadOnlyList<Guid>> ExistingEvidence(IntelligenceProcessingContext c, CancellationToken ct)
    {
        using var db = connections.Create();
        return (await db.QueryAsync<Guid>(new CommandDefinition("SELECT id FROM valorapesquisa.evidence_items WHERE organization_id=@OrganizationId AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 500", new { c.OrganizationId }, cancellationToken: ct))).ToList();
    }
}
