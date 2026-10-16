using System.Text.Json;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DiagnosticWorkspace;

namespace Valora.Infrastructure.Repositories;

public sealed class DiagnosticWorkspaceRepository(IDbConnectionFactory connections, IDbTransactionFactory transactions) : IDiagnosticWorkspaceRepository {
    private static readonly IReadOnlyDictionary<string, string> Tables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["responses"] = "responses",
        ["metrics"] = "metric_values",
        ["indices"] = "index_values",
        ["inferences"] = "inference_results",
        ["insights"] = "insights",
        ["heatmap"] = "heatmap_snapshots",
        ["radar"] = "radar_snapshots",
        ["evolution"] = "evolution_cycles",
        ["journey"] = "journey_events",
        ["benchmark"] = "benchmark_runs",
        ["report"] = "executive_reports",
        ["governance"] = "platform_governance_events"
    };
    public async Task<DiagnosticCycleDto?> GetCycleAsync(Guid organizationId, Guid id, CancellationToken ct) {
        const string sql = """
        SELECT coalesce(dc.id,s.id) Id,s.organization_id OrganizationId,s.id SurveyId,s.form_id FormId,s.title,
          s.description,coalesce(dc.cycle_number,1) CycleNumber,coalesce(dc.methodology_version,'1') MethodologyVersion,
          coalesce(dc.status,s.status) Status,coalesce(dc.opened_at,s.starts_at) OpenedAt,dc.published_at PublishedAt,
          coalesce(dc.closed_at,s.expires_at) ClosedAt,dc.processed_at ProcessedAt,dc.report_generated_at ReportGeneratedAt,
          (SELECT count(*)::int FROM valorapesquisa.responses r WHERE r.organization_id=s.organization_id AND r.survey_id=s.id AND coalesce(r.is_deleted,false)=false) ResponseCount,
          (SELECT count(*)::int FROM valorapesquisa.evidence_items e WHERE e.organization_id=s.organization_id AND e.survey_id=s.id AND e.deleted_at IS NULL) EvidenceCount,
          (SELECT count(*)::int FROM valorapesquisa.evidence_items e WHERE e.organization_id=s.organization_id AND e.survey_id=s.id AND e.deleted_at IS NULL AND coalesce(e.mapping_status,e.metadata_json->>'mappingStatus')='mapped') MappedEvidenceCount,
          (SELECT count(*)::int FROM valorapesquisa.evidence_items e WHERE e.organization_id=s.organization_id AND e.survey_id=s.id AND e.deleted_at IS NULL AND coalesce(e.mapping_status,e.metadata_json->>'mappingStatus','pending_mapping')<>'mapped') PendingEvidenceCount,
          dc.confidence_level ConfidenceLevel,coalesce(dc.processing_status,'pending') ProcessingStatus,s.public_url PublicUrl,
          coalesce(dc.created_at,s.created_at) CreatedAt,coalesce(dc.updated_at,s.updated_at,s.created_at) UpdatedAt,
          f.name FormName, fv.version FormVersionNumber, s.starts_at StartsAt, s.expires_at ExpiresAt,
          coalesce(nullif(to_jsonb(dc)->>'target_audience',''), nullif(to_jsonb(s)->>'target_audience',''), nullif(dc.metadata_json->>'targetAudience',''), nullif(dc.metadata_json->>'target_audience',''), (SELECT nullif(camp.audience_json->>'description','') FROM valorapesquisa.diagnostic_campaigns camp WHERE camp.organization_id=s.organization_id AND camp.survey_id=s.id AND camp.deleted_at IS NULL ORDER BY camp.created_at DESC LIMIT 1)) TargetAudience,
          coalesce((SELECT channel FROM valorapesquisa.diagnostic_campaigns camp WHERE camp.organization_id=s.organization_id AND camp.survey_id=s.id ORDER BY camp.created_at DESC LIMIT 1), 'manual') DistributionChannel,
          (SELECT u.name FROM valorapesquisa.users u WHERE u.id=coalesce((to_jsonb(dc)->>'responsible_user_id')::uuid, (to_jsonb(s)->>'responsible_user_id')::uuid, (dc.metadata_json->>'responsibleUserId')::uuid, (dc.metadata_json->>'responsible_user_id')::uuid) AND u.organization_id=s.organization_id AND u.deleted_at IS NULL) ResponsibleName,
          coalesce((to_jsonb(dc)->>'responsible_user_id')::uuid, (to_jsonb(s)->>'responsible_user_id')::uuid, (dc.metadata_json->>'responsibleUserId')::uuid, (dc.metadata_json->>'responsible_user_id')::uuid) ResponsibleUserId,
          s.form_version_id FormVersionId,
          coalesce(nullif(to_jsonb(s)->>'row_version','')::int, nullif(to_jsonb(dc)->>'row_version','')::int, 1) RowVersion
        FROM valorapesquisa.surveys s
        LEFT JOIN valorapesquisa.diagnostic_cycles dc ON dc.survey_id=s.id AND dc.organization_id=s.organization_id AND dc.deleted_at IS NULL
        LEFT JOIN valorapesquisa.form_versions fv ON fv.id=s.form_version_id
        LEFT JOIN valorapesquisa.forms f ON f.id=coalesce(s.form_id, fv.form_id)
        WHERE s.organization_id=@organizationId AND (s.id=@id OR dc.id=@id) AND coalesce(s.is_deleted,false)=false LIMIT 1
        """;
        using var c = connections.Create(); return await c.QuerySingleOrDefaultAsync<DiagnosticCycleDto>(new CommandDefinition(sql, new { organizationId, id }, cancellationToken: ct));
    }
    public async Task<IReadOnlyList<DiagnosticWorkspaceEvidenceDto>> EvidenceAsync(Guid organizationId, Guid surveyId, CancellationToken ct) {
        const string sql = """SELECT id,response_id ResponseId,question_id QuestionId,concept_code ConceptCode,metric_code MetricCode,index_code IndexCode,evidence_type EvidenceType,normalized_value NormalizedValue,weight,confidence_weight ConfidenceWeight,coalesce(mapping_status,metadata_json->>'mappingStatus','pending_mapping') MappingStatus,CASE WHEN text_excerpt IS NULL THEN NULL ELSE regexp_replace(text_excerpt,'.(?=.{4})','•','g') END MaskedAnswer,created_at CreatedAt FROM valorapesquisa.evidence_items WHERE organization_id=@organizationId AND survey_id=@surveyId AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 500""";
        using var c = connections.Create(); return (await c.QueryAsync<DiagnosticWorkspaceEvidenceDto>(new CommandDefinition(sql, new { organizationId, surveyId }, cancellationToken: ct))).ToList();
    }
    public async Task<DiagnosticWorkspaceExecutionDto> ExecutionAsync(Guid organizationId, DiagnosticCycleDto cycle, Guid userId, bool organizationWide, CancellationToken ct) {
        const string planScope = "p.organization_id=@organizationId AND p.deleted_at IS NULL AND EXISTS(SELECT 1 FROM valorapesquisa.organization_modules om_scope WHERE om_scope.organization_id=@organizationId AND om_scope.module_code='organizational_intelligence' AND om_scope.enabled) AND (@organizationWide OR p.owner_user_id IS NULL OR p.owner_user_id=@userId OR EXISTS(SELECT 1 FROM valorapesquisa.action_items ai_scope WHERE ai_scope.action_plan_id=p.id AND ai_scope.organization_id=p.organization_id AND ai_scope.deleted_at IS NULL AND ai_scope.responsible_user_id=@userId))";
        var args = new { organizationId, cycleId = cycle.Id, surveyId = cycle.SurveyId, userId, organizationWide };
        var planSql = $"""
        WITH diagnostic_refs AS (
            SELECT @cycleId::uuid id UNION SELECT @surveyId::uuid id
        ), direct_plans AS (
            SELECT DISTINCT p.id
            FROM valorapesquisa.action_plans p
            WHERE {planScope}
              AND (
                p.diagnostic_id IN (SELECT id FROM diagnostic_refs)
                OR (p.origin_type='diagnostic' AND p.origin_id IN (SELECT id FROM diagnostic_refs))
              )
        ), linked_plans AS (
            SELECT id FROM direct_plans
            UNION
            SELECT DISTINCT p.id
            FROM valorapesquisa.action_plans p
            WHERE {planScope}
              AND EXISTS (
                    SELECT 1 FROM valorapesquisa.action_items i
                    WHERE i.action_plan_id=p.id AND i.organization_id=p.organization_id AND i.deleted_at IS NULL
                      AND (i.diagnostic_id IN (SELECT id FROM diagnostic_refs) OR (i.origin_type='diagnostic' AND i.origin_id IN (SELECT id FROM diagnostic_refs)))
                )
        ), linked_items AS (
            SELECT DISTINCT i.id
            FROM valorapesquisa.action_items i
            JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id
            WHERE i.organization_id=@organizationId AND i.deleted_at IS NULL AND {planScope}
              AND (
                i.diagnostic_id IN (SELECT id FROM diagnostic_refs)
                OR (i.origin_type='diagnostic' AND i.origin_id IN (SELECT id FROM diagnostic_refs))
                OR p.id IN (SELECT id FROM direct_plans)
              )
        )
        SELECT p.id,p.title,p.status,p.priority,p.origin_type OriginType,p.owner_user_id OwnerUserId,u.name OwnerName,p.due_at DueAt,
          CASE WHEN count(i.id) FILTER(WHERE i.status<>'canceled')=0 THEN 0 ELSE coalesce(round(avg(i.progress_percent) FILTER(WHERE i.status<>'canceled'))::int,0) END ProgressPercent,
          count(i.id) FILTER(WHERE i.status NOT IN('completed','canceled'))::int ActiveItemCount,p.created_at CreatedAt
        FROM valorapesquisa.action_plans p
        LEFT JOIN valorapesquisa.action_items i ON i.action_plan_id=p.id AND i.organization_id=p.organization_id AND i.deleted_at IS NULL
        LEFT JOIN valorapesquisa.users u ON u.id=p.owner_user_id AND u.organization_id=p.organization_id AND u.deleted_at IS NULL
        WHERE p.id IN (SELECT id FROM linked_plans)
        GROUP BY p.id,u.name
        ORDER BY p.created_at DESC,p.id DESC
        LIMIT 51;
        """;
        var itemSql = $"""
        WITH diagnostic_refs AS (
            SELECT @cycleId::uuid id UNION SELECT @surveyId::uuid id
        ), direct_plans AS (
            SELECT DISTINCT p.id
            FROM valorapesquisa.action_plans p
            WHERE {planScope}
              AND (
                p.diagnostic_id IN (SELECT id FROM diagnostic_refs)
                OR (p.origin_type='diagnostic' AND p.origin_id IN (SELECT id FROM diagnostic_refs))
              )
        ), linked_plans AS (
            SELECT id FROM direct_plans
            UNION
            SELECT DISTINCT p.id
            FROM valorapesquisa.action_plans p
            WHERE {planScope}
              AND EXISTS (
                    SELECT 1 FROM valorapesquisa.action_items i
                    WHERE i.action_plan_id=p.id AND i.organization_id=p.organization_id AND i.deleted_at IS NULL
                      AND (i.diagnostic_id IN (SELECT id FROM diagnostic_refs) OR (i.origin_type='diagnostic' AND i.origin_id IN (SELECT id FROM diagnostic_refs)))
                )
        ), linked_items AS (
            SELECT DISTINCT i.id
            FROM valorapesquisa.action_items i
            JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id
            WHERE i.organization_id=@organizationId AND i.deleted_at IS NULL AND {planScope}
              AND (
                i.diagnostic_id IN (SELECT id FROM diagnostic_refs)
                OR (i.origin_type='diagnostic' AND i.origin_id IN (SELECT id FROM diagnostic_refs))
                OR p.id IN (SELECT id FROM direct_plans)
              )
        )
        SELECT i.id,i.action_plan_id ActionPlanId,p.title PlanTitle,i.title,i.status,i.priority,i.responsible_user_id ResponsibleUserId,u.name ResponsibleName,i.due_at DueAt,
          i.progress_percent ProgressPercent,i.evidence_summary EvidenceSummary,i.expected_outcome ExpectedOutcome,i.completion_evidence CompletionEvidence,
          i.metadata_json->>'completionResult' CompletionResult,
          ((i.due_at AT TIME ZONE org.time_zone)::date < (now() AT TIME ZONE org.time_zone)::date AND i.status NOT IN('completed','canceled')) IsOverdue,
          i.created_at CreatedAt
        FROM valorapesquisa.action_items i
        JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id
        JOIN valorapesquisa.organizations org ON org.id=i.organization_id
        LEFT JOIN valorapesquisa.users u ON u.id=i.responsible_user_id AND u.organization_id=i.organization_id AND u.deleted_at IS NULL
        WHERE i.id IN (SELECT id FROM linked_items)
        ORDER BY i.due_at NULLS LAST,i.created_at DESC,i.id DESC
        LIMIT 101;
        """;
        var historySql = $"""
        WITH diagnostic_refs AS (
            SELECT @cycleId::uuid id UNION SELECT @surveyId::uuid id
        ), direct_plans AS (
            SELECT DISTINCT p.id
            FROM valorapesquisa.action_plans p
            WHERE {planScope}
              AND (
                p.diagnostic_id IN (SELECT id FROM diagnostic_refs)
                OR (p.origin_type='diagnostic' AND p.origin_id IN (SELECT id FROM diagnostic_refs))
              )
        ), linked_plans AS (
            SELECT id FROM direct_plans
            UNION
            SELECT DISTINCT p.id
            FROM valorapesquisa.action_plans p
            WHERE {planScope}
              AND EXISTS (
                    SELECT 1 FROM valorapesquisa.action_items i
                    WHERE i.action_plan_id=p.id AND i.organization_id=p.organization_id AND i.deleted_at IS NULL
                      AND (i.diagnostic_id IN (SELECT id FROM diagnostic_refs) OR (i.origin_type='diagnostic' AND i.origin_id IN (SELECT id FROM diagnostic_refs)))
                )
        ), linked_items AS (
            SELECT DISTINCT i.id
            FROM valorapesquisa.action_items i
            JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id
            WHERE i.organization_id=@organizationId AND i.deleted_at IS NULL AND {planScope}
              AND (
                i.diagnostic_id IN (SELECT id FROM diagnostic_refs)
                OR (i.origin_type='diagnostic' AND i.origin_id IN (SELECT id FROM diagnostic_refs))
                OR p.id IN (SELECT id FROM direct_plans)
              )
        )
        SELECT 'plan' Kind,p.id EntityId,p.title,h.operation Operation,h.from_value FromValue,h.to_value ToValue,h.reason,u.name ActorName,h.changed_at ChangedAt
        FROM valorapesquisa.action_plan_change_history h
        JOIN valorapesquisa.action_plans p ON p.id=h.action_plan_id
        LEFT JOIN valorapesquisa.users u ON u.id=h.changed_by_user_id
        WHERE p.id IN (SELECT id FROM linked_plans)
        UNION ALL
        SELECT 'item' Kind,i.id EntityId,i.title,h.operation,coalesce(h.from_value,h.from_status) FromValue,coalesce(h.to_value,h.to_status) ToValue,h.reason,u.name ActorName,h.changed_at ChangedAt
        FROM valorapesquisa.action_item_status_history h
        JOIN valorapesquisa.action_items i ON i.id=h.action_item_id
        LEFT JOIN valorapesquisa.users u ON u.id=h.changed_by_user_id
        WHERE i.id IN (SELECT id FROM linked_items)
        ORDER BY ChangedAt DESC,EntityId DESC
        LIMIT 101;
        """;
        using var c = connections.Create();
        var plans = (await c.QueryAsync<DiagnosticLinkedPlanDto>(new CommandDefinition(planSql, args, cancellationToken: ct))).AsList();
        var items = (await c.QueryAsync<DiagnosticLinkedActionItemDto>(new CommandDefinition(itemSql, args, cancellationToken: ct))).AsList();
        var history = (await c.QueryAsync<DiagnosticExecutionHistoryDto>(new CommandDefinition(historySql, args, cancellationToken: ct))).AsList();
        return new(plans.Take(50).ToList(), items.Take(100).ToList(), history.Take(100).ToList(),
            plans.Count > 50, items.Count > 100, history.Count > 100);
    }
    public async Task<IReadOnlyList<DiagnosticWorkspaceItemDto>> ModuleAsync(Guid organizationId, Guid cycleId, Guid surveyId, string module, CancellationToken ct) {
        string sql;
        if (module.Equals("actions", StringComparison.OrdinalIgnoreCase)) sql = "SELECT id,code,status,to_jsonb(x)::text DataJson,created_at CreatedAt,updated_at UpdatedAt FROM valorapesquisa.valora_actions x WHERE organization_id=@organizationId AND survey_id=@surveyId AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 200";
        else if (module.Equals("alerts", StringComparison.OrdinalIgnoreCase)) sql = "SELECT id,indicator_code code,status,to_jsonb(x)::text DataJson,created_at CreatedAt,updated_at UpdatedAt FROM valorapesquisa.intelligent_alerts x WHERE organization_id=@organizationId AND survey_id=@surveyId AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 200";
        else if (module.Equals("evolution", StringComparison.OrdinalIgnoreCase)) sql = """
            SELECT x.id,NULL::text code,x.status,
              (to_jsonb(x)||jsonb_build_object(
                'timeZone',(SELECT time_zone FROM valorapesquisa.organizations WHERE id=x.organization_id),
                'lastMonitoringAt',(SELECT max(es.calculated_at) FROM valorapesquisa.evolution_snapshots es WHERE es.organization_id=x.organization_id AND es.evolution_cycle_id=x.id AND es.snapshot_type='monitoring' AND es.deleted_at IS NULL),
                'lastReevaluationAt',(SELECT max(es.calculated_at) FROM valorapesquisa.evolution_snapshots es WHERE es.organization_id=x.organization_id AND es.evolution_cycle_id=x.id AND es.snapshot_type='reevaluation' AND es.deleted_at IS NULL),
                'referenceReviewRequired',x.baseline_measurement_id IS NULL AND (x.result_id IS NOT NULL OR coalesce(x.metadata_json->>'baselineSource','') NOT IN('manual','manual_informed')) AND x.metadata_json->>'referenceSchemaVersion' IS DISTINCT FROM '2'))::text DataJson,
              x.created_at CreatedAt,x.updated_at UpdatedAt
            FROM valorapesquisa.evolution_cycles x
            WHERE x.organization_id=@organizationId AND (x.diagnostic_id IN(@cycleId,@surveyId) OR x.cycle_id=@cycleId OR x.survey_id=@surveyId) AND x.deleted_at IS NULL
            ORDER BY x.created_at DESC LIMIT 200
            """;
        else if (module.Equals("participation", StringComparison.OrdinalIgnoreCase)) sql = """
            SELECT
              coalesce((SELECT id FROM valorapesquisa.diagnostic_participation_snapshots WHERE organization_id=@organizationId AND (cycle_id=@cycleId OR survey_id=@surveyId) AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 1), gen_random_uuid()) Id,
              'participation'::text Code,
              'computed'::text Status,
              jsonb_build_object(
                'respostasRecebidas', (SELECT count(*)::int FROM valorapesquisa.responses WHERE organization_id=@organizationId AND survey_id=@surveyId AND coalesce(is_deleted,false)=false),
                'respostasValidas', (SELECT count(*)::int FROM valorapesquisa.responses WHERE organization_id=@organizationId AND survey_id=@surveyId AND status='completed' AND coalesce(is_deleted,false)=false),
                'respostasPontuadas', (SELECT count(DISTINCT r.id)::int FROM valorapesquisa.result_scores rs JOIN valorapesquisa.responses r ON r.id=rs.response_id WHERE r.organization_id=@organizationId AND r.survey_id=@surveyId AND coalesce(r.is_deleted,false)=false),
                'pendenciasProcessamento', (SELECT count(*)::int FROM valorapesquisa.intelligence_processing_jobs WHERE organization_id=@organizationId AND survey_id=@surveyId AND status IN ('pending','running','retry_scheduled') AND deleted_at IS NULL),
                'destinatariosEmail', (
                    SELECT count(DISTINCT coalesce(nullif(hash,''), id::text))::int FROM (
                        SELECT lower(trim(coalesce(nullif(dcr.email_hash,''), nullif(dcr.recipient_reference,''), dcr.id::text))) as hash, dcr.id
                        FROM valorapesquisa.diagnostic_campaign_recipients dcr
                        JOIN valorapesquisa.diagnostic_campaigns dc ON dc.id=dcr.campaign_id
                        WHERE dc.organization_id=@organizationId AND dc.survey_id=@surveyId AND dcr.deleted_at IS NULL
                        UNION ALL
                        SELECT lower(trim(coalesce(nullif(si.email_hash,''), si.id::text))) as hash, si.id
                        FROM valorapesquisa.survey_invites si
                        WHERE si.organization_id=@organizationId AND si.survey_id=@surveyId
                    ) t
                ),
                'mensagensFila', (
                    SELECT count(*)::int FROM valorapesquisa.email_jobs ej
                    WHERE ej.organization_id=@organizationId
                      AND ej.status IN ('pending','queued')
                      AND (
                        ej.id IN (
                            SELECT dcr.email_job_id FROM valorapesquisa.diagnostic_campaign_recipients dcr
                            JOIN valorapesquisa.diagnostic_campaigns dc ON dc.id=dcr.campaign_id
                            WHERE dc.organization_id=@organizationId AND dc.survey_id=@surveyId AND dcr.email_job_id IS NOT NULL
                        )
                        OR ej.response_id IN (
                            SELECT r.id FROM valorapesquisa.responses r
                            WHERE r.organization_id=@organizationId AND r.survey_id=@surveyId
                        )
                        OR (ej.payload_json->>'surveyId')::text = @surveyId::text
                      )
                ),
                'enviosRegistrados', (
                    SELECT count(*)::int FROM valorapesquisa.email_jobs ej
                    WHERE ej.organization_id=@organizationId
                      AND ej.status IN ('sent','delivered')
                      AND (
                        ej.id IN (
                            SELECT dcr.email_job_id FROM valorapesquisa.diagnostic_campaign_recipients dcr
                            JOIN valorapesquisa.diagnostic_campaigns dc ON dc.id=dcr.campaign_id
                            WHERE dc.organization_id=@organizationId AND dc.survey_id=@surveyId AND dcr.email_job_id IS NOT NULL
                        )
                        OR ej.response_id IN (
                            SELECT r.id FROM valorapesquisa.responses r
                            WHERE r.organization_id=@organizationId AND r.survey_id=@surveyId
                        )
                        OR (ej.payload_json->>'surveyId')::text = @surveyId::text
                      )
                ),
                'falhas', (
                    SELECT count(*)::int FROM valorapesquisa.email_jobs ej
                    WHERE ej.organization_id=@organizationId
                      AND ej.status IN ('failed','undelivered','bounced')
                      AND (
                        ej.id IN (
                            SELECT dcr.email_job_id FROM valorapesquisa.diagnostic_campaign_recipients dcr
                            JOIN valorapesquisa.diagnostic_campaigns dc ON dc.id=dcr.campaign_id
                            WHERE dc.organization_id=@organizationId AND dc.survey_id=@surveyId AND dcr.email_job_id IS NOT NULL
                        )
                        OR ej.response_id IN (
                            SELECT r.id FROM valorapesquisa.responses r
                            WHERE r.organization_id=@organizationId AND r.survey_id=@surveyId
                        )
                        OR (ej.payload_json->>'surveyId')::text = @surveyId::text
                      )
                ),
                'origens', jsonb_build_object(
                    'respostasRecebidas', 'Respostas registradas com vínculo direto a este diagnóstico.',
                    'respostasValidas', 'Respostas concluídas (status completed) associadas exclusivamente a este diagnóstico.',
                    'respostasPontuadas', 'Respostas distintas com scores de maturidade apurados.',
                    'pendenciasProcessamento', 'Tarefas de inteligência analítica pendentes ou em reprocessamento.',
                    'destinatariosEmail', 'Destinatários únicos consolidados de campanhas e convites deste diagnóstico.',
                    'mensagensFila', 'Disparos na fila vinculados às campanhas ou respostas deste diagnóstico.',
                    'enviosRegistrados', 'Disparos concluídos com sucesso para este diagnóstico.',
                    'falhas', 'Disparos com falha ou devolução registrados para este diagnóstico.'
                )
              )::text DataJson,
              now() CreatedAt,
              now() UpdatedAt
            """;
        else if (module.Equals("responses", StringComparison.OrdinalIgnoreCase)) sql = """
            SELECT x.id,NULL::text code,x.status,
              jsonb_build_object(
                'id',x.id,
                'surveyId',x.survey_id,
                'status',x.status,
                'submittedAt',x.submitted_at,
                'completedAt',x.completed_at,
                'createdAt',x.created_at,
                'updatedAt',coalesce(x.updated_at,x.created_at),
                'hasQualitativeAnswer',NULLIF(x.qualitative,'') IS NOT NULL,
                'answerCount',(SELECT count(*)::int FROM valorapesquisa.response_answers a WHERE a.response_id=x.id)
              )::text DataJson,
              x.created_at CreatedAt,coalesce(x.updated_at,x.created_at) UpdatedAt
            FROM valorapesquisa.responses x
            WHERE x.organization_id=@organizationId AND x.survey_id=@surveyId AND coalesce(x.is_deleted,false)=false
            ORDER BY x.created_at DESC LIMIT 200
            """;
        else if (module.Equals("insights", StringComparison.OrdinalIgnoreCase)) sql = """
            SELECT x.id, x.code, x.status,
              (to_jsonb(x.data) || jsonb_build_object(
                'id', x.id,
                'code', x.code,
                'originType', 'insight',
                'status', x.status,
                'title', coalesce(x.data->>'title', x.code, 'Achado analítico'),
                'description', coalesce(x.data->>'executiveSummary', x.data->>'description', x.data->>'probableCause', 'Achado gerado a partir de evidências do diagnóstico.'),
                'dimension', coalesce(x.data->>'dimension', x.data->>'related_dimension', 'Maturidade Organizacional'),
                'evidenceCount', coalesce(jsonb_array_length(case when jsonb_typeof(x.data->'evidenceIds')='array' then x.data->'evidenceIds' else '[]'::jsonb end), 0),
                'limitation', coalesce(x.data->>'limitation', 'Sustentado exclusivamente pelas evidências convergentes da amostra coletada.'),
                'nature', coalesce(x.data->>'nature', case when x.status IN ('approved','reviewed','converted_to_action') then 'decisao_revisada' when x.code IN ('hypothesis','inference') then 'hipotese' when x.data->>'calculatedScore' is not null then 'fato_calculado' else 'sugestao_ia' end),
                'confidence', x.data->>'confidence',
                'recommendation', coalesce(x.data->>'recommendation', 'Revisar metodologicamente e converter em plano de ação rastreável.'),
                'reviewStatus', coalesce(x.data->>'reviewStatus', x.status, 'pending_review'),
                'linkedPlanId', (SELECT ap.id FROM valorapesquisa.action_plans ap WHERE ap.organization_id=@organizationId AND ap.origin_id=x.id AND ap.deleted_at IS NULL ORDER BY ap.created_at DESC LIMIT 1)
              ))::text DataJson,
              x.created_at CreatedAt, x.updated_at UpdatedAt
            FROM valorapesquisa.insights x
            WHERE x.organization_id=@organizationId AND (x.survey_id=@surveyId OR x.survey_id=@cycleId) AND x.deleted_at IS NULL
            UNION ALL
            SELECT vi.id, vi.insight_type Code, vi.status,
              jsonb_build_object(
                'id', vi.id,
                'code', vi.insight_type,
                'originType', 'valora_ai_insight',
                'status', vi.status,
                'title', vi.title,
                'description', vi.summary,
                'dimension', coalesce(vi.related_dimension, 'Geral'),
                'evidenceSummary', vi.evidence_summary,
                'limitation', coalesce(vi.limitation, 'Sustentado pelas evidências vinculadas ao pacote analítico.'),
                'nature', case when vi.status IN ('approved','reviewed','converted_to_action') then 'decisao_revisada' else 'sugestao_ia' end,
                'confidence', vi.confidence_level,
                'recommendation', vi.recommendation,
                'reviewStatus', vi.status,
                'reviewVersion', vi.review_version,
                'reviewedAt', vi.reviewed_at,
                'linkedPlanId', (SELECT ap.id FROM valorapesquisa.action_plans ap WHERE ap.organization_id=@organizationId AND ap.origin_type='ai_insight' AND ap.origin_id=vi.id AND ap.deleted_at IS NULL LIMIT 1)
              )::text DataJson,
              vi.created_at CreatedAt, vi.updated_at UpdatedAt
            FROM valorapesquisa.valora_ai_insights vi
            WHERE vi.organization_id=@organizationId AND vi.diagnostic_id=@surveyId AND vi.deleted_at IS NULL
            ORDER BY CreatedAt DESC LIMIT 200
            """;
        else { if (!Tables.TryGetValue(module, out var table)) throw new ArgumentException("Módulo inválido.", nameof(module)); sql = $"SELECT id,code,status,to_jsonb(x)::text DataJson,created_at CreatedAt,updated_at UpdatedAt FROM valorapesquisa.{table} x WHERE organization_id=@organizationId AND (cycle_id=@cycleId OR survey_id=@surveyId) AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 200"; }
        using var c = connections.Create(); var rows = await c.QueryAsync<Row>(new CommandDefinition(sql, new { organizationId, cycleId, surveyId }, cancellationToken: ct));
        return rows.Select(x => new DiagnosticWorkspaceItemDto(x.Id, x.Code, x.Status, JsonSerializer.Deserialize<JsonElement>(x.DataJson), x.CreatedAt, x.UpdatedAt)).ToList();
    }
    public async Task<DiagnosticWorkspaceItemDto?> LastJobAsync(Guid organizationId, Guid surveyId, CancellationToken ct) { using var c = connections.Create(); var x = await c.QuerySingleOrDefaultAsync<Row>(new CommandDefinition("SELECT id,trigger code,status,to_jsonb(x)::text DataJson,created_at CreatedAt,updated_at UpdatedAt FROM valorapesquisa.intelligence_processing_jobs x WHERE organization_id=@organizationId AND survey_id=@surveyId AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 1", new { organizationId, surveyId }, cancellationToken: ct)); return x is null ? null : new(x.Id, x.Code, x.Status, JsonSerializer.Deserialize<JsonElement>(x.DataJson), x.CreatedAt, x.UpdatedAt); }
    public async Task<bool> HasActiveJobAsync(Guid o, Guid s, CancellationToken ct) { using var c = connections.Create(); return await c.ExecuteScalarAsync<bool>(new CommandDefinition("SELECT EXISTS(SELECT 1 FROM valorapesquisa.intelligence_processing_jobs WHERE organization_id=@o AND survey_id=@s AND status IN ('pending','running','retry_scheduled') AND deleted_at IS NULL)", new { o, s }, cancellationToken: ct)); }
    public async Task<Guid> MarkProcessingAsync(Guid o, DiagnosticCycleDto d, Guid user, string correlation, CancellationToken ct) { await using var u = await transactions.BeginAsync(ct); try { var cycle = await EnsureCycle(u.Connection, u.Transaction, o, d, user, ct); var job = Guid.NewGuid(); await u.Connection.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.diagnostic_cycles SET processing_status='pending',updated_at=now() WHERE id=@cycle AND organization_id=@o; INSERT INTO valorapesquisa.intelligence_processing_jobs(id,organization_id,survey_id,form_id,source_entity_id,trigger,status,idempotency_key,correlation_id) VALUES(@job,@o,@survey,@form,@cycle,'manual_recalculation','pending',@key,@correlation)", new { o, cycle, job, survey = d.SurveyId, form = d.FormId, key = $"diagnostic:{d.SurveyId}:manual:{DateTime.UtcNow:yyyyMMddHHmm}", correlation }, u.Transaction, cancellationToken: ct)); await Audit(u, o, cycle, user, "diagnostic.processing.requested", ct); await u.CommitAsync(); return job; } catch { await u.RollbackAsync(); throw; } }
    public async Task<DiagnosticWorkspaceCommandDto?> CloseAsync(Guid o, DiagnosticCycleDto d, Guid user, string correlation, CancellationToken ct) { if (d.Status is "draft" or "cancelled" or "archived") return new(d.Id, d.Status, "Publique o diagnóstico antes de encerrar o ciclo."); if (d.Status == "closed") return new(d.Id, "closed", "Este ciclo já está encerrado."); await using var u = await transactions.BeginAsync(ct); try { var cycle = await EnsureCycle(u.Connection, u.Transaction, o, d, user, ct); var job = Guid.NewGuid(); var limited = d.ResponseCount == 0; await u.Connection.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.diagnostic_cycles SET status='closed',closed_at=now(),processing_status='pending',metadata_json=metadata_json||jsonb_build_object('insufficientData',@limited),updated_at=now() WHERE id=@cycle AND organization_id=@o; UPDATE valorapesquisa.surveys SET status='closed',expires_at=now(),updated_at=now() WHERE id=@survey AND organization_id=@o; INSERT INTO valorapesquisa.intelligence_processing_jobs(id,organization_id,survey_id,form_id,source_entity_id,trigger,status,idempotency_key,correlation_id) VALUES(@job,@o,@survey,@form,@cycle,'diagnosis_closed','pending',@key,@correlation); INSERT INTO valorapesquisa.journey_events(organization_id,survey_id,cycle_id,code,status,data) VALUES(@o,@survey,@cycle,'diagnostic_closed','completed',jsonb_build_object('limited',@limited)); INSERT INTO valorapesquisa.platform_governance_events(organization_id,survey_id,cycle_id,code,status,data) VALUES(@o,@survey,@cycle,'diagnostic.closed','completed',jsonb_build_object('userId',@user)); INSERT INTO valorapesquisa.notifications(organization_id,user_id,title,message,type,related_module,related_entity_id) SELECT @o,id,'Ciclo diagnóstico encerrado','O processamento final foi enfileirado.','diagnostic_closed','diagnostic-workspace',@cycle FROM valorapesquisa.users WHERE organization_id=@o AND is_deleted=false", new { o, cycle, job, survey = d.SurveyId, form = d.FormId, user, limited, key = $"diagnostic:{d.SurveyId}:close", correlation }, u.Transaction, cancellationToken: ct)); await u.CommitAsync(); return new(cycle, "closed", limited ? "Ciclo encerrado sem respostas; a limitação foi registrada." : "Ciclo encerrado e processamento final enfileirado.", job, limited); } catch { await u.RollbackAsync(); throw; } }
    public async Task<DiagnosticWorkspaceCommandDto> GenerateReportAsync(Guid o, DiagnosticCycleDto d, Guid user, bool preview, CancellationToken ct) {
        await using var u = await transactions.BeginAsync(ct);
        try {
            var cycle = await EnsureCycle(u.Connection, u.Transaction, o, d, user, ct);
            // The workspace no longer fabricates an executive_reports row. Formal
            // deliverables need an eligible immutable result, artifact generation,
            // review and publication through ExecutiveDeliveryService.
            await Audit(u, o, cycle, user, preview ? "report.preview.requested" : "report.delivery.requested", ct);
            await u.CommitAsync();
            return new(cycle, "requires_preparation",
                "Abra a Central de Entregas para selecionar um resultado elegível, gerar o artefato e submetê-lo à revisão. A solicitação não criou nem publicou um relatório.",
                HasLimitation: d.EvidenceCount == 0);
        }
        catch { await u.RollbackAsync(); throw; }
    }
    public async Task<DiagnosticWorkspacePreparationOptionsDto> GetPreparationOptionsAsync(Guid organizationId, Guid surveyId, CancellationToken ct) {
        using var c = connections.Create();
        var users = (await c.QueryAsync<EligibleUserDto>(new CommandDefinition(
            """
            SELECT u.id, u.name, u.email, NULL as Department
            FROM valorapesquisa.users u
            WHERE u.organization_id=@organizationId AND u.status='active' AND coalesce(u.is_deleted,false)=false
            ORDER BY u.name ASC
            """, new { organizationId }, cancellationToken: ct))).ToList();

        var surveyInfo = await c.QuerySingleOrDefaultAsync<(Guid FormId, Guid FormVersionId, string Status, int ResponseCount)>(new CommandDefinition(
            """
            SELECT coalesce(s.form_id, fv.form_id) FormId, s.form_version_id FormVersionId, s.status Status,
                   (SELECT count(*)::int FROM valorapesquisa.responses r WHERE r.organization_id=@organizationId AND r.survey_id=@surveyId AND coalesce(r.is_deleted,false)=false) ResponseCount
            FROM valorapesquisa.surveys s
            LEFT JOIN valorapesquisa.form_versions fv ON fv.id=s.form_version_id
            WHERE s.organization_id=@organizationId AND s.id=@surveyId AND coalesce(s.is_deleted,false)=false
            LIMIT 1
            """, new { organizationId, surveyId }, cancellationToken: ct));

        var versions = (await c.QueryAsync<FormVersionOptionDto>(new CommandDefinition(
            """
            SELECT fv.id, fv.version, f.name Title, fv.status, fv.is_immutable IsImmutable, fv.created_at CreatedAt
            FROM valorapesquisa.form_versions fv
            JOIN valorapesquisa.forms f ON f.id=fv.form_id
            WHERE fv.form_id=@formId AND fv.status='published'
            ORDER BY fv.version DESC
            """, new { formId = surveyInfo.FormId }, cancellationToken: ct))).ToList();

        bool canChange = surveyInfo.Status == "draft" && surveyInfo.ResponseCount == 0;
        string? lockReason = surveyInfo.ResponseCount > 0
            ? $"Metodologia fixada porque já existem {surveyInfo.ResponseCount} respostas coletadas neste ciclo."
            : surveyInfo.Status != "draft"
                ? "A metodologia só pode ser alterada enquanto o diagnóstico estiver em rascunho."
                : null;

        return new DiagnosticWorkspacePreparationOptionsDto(users, versions, canChange, lockReason);
    }

    public async Task<DiagnosticCycleDto> UpdatePreparationAsync(Guid organizationId, Guid surveyId, UpdateDiagnosticPreparationDto dto, Guid userId, string correlationId, CancellationToken ct) {
        if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Trim().Length < 3)
            throw new ArgumentException("O título do diagnóstico é obrigatório e deve ter no mínimo 3 caracteres.", nameof(dto.Title));

        if (dto.StartsAt.HasValue && dto.ExpiresAt.HasValue && dto.ExpiresAt.Value <= dto.StartsAt.Value)
            throw new ArgumentException("O prazo final de coleta deve ser posterior à data de início.", nameof(dto.ExpiresAt));

        await using var u = await transactions.BeginAsync(ct);
        try {
            var current = await u.Connection.QuerySingleOrDefaultAsync<(Guid Id, Guid FormId, Guid FormVersionId, string Status, int ResponseCount, int RowVersion)>(new CommandDefinition(
                """
                SELECT s.id,
                       coalesce(s.form_id, (SELECT fv.form_id FROM valorapesquisa.form_versions fv WHERE fv.id=s.form_version_id)) FormId,
                       s.form_version_id FormVersionId, s.status Status,
                       (SELECT count(*)::int FROM valorapesquisa.responses r WHERE r.organization_id=@organizationId AND r.survey_id=@surveyId AND coalesce(r.is_deleted,false)=false) ResponseCount,
                       coalesce(nullif(to_jsonb(s)->>'row_version','')::int, 1) RowVersion
                FROM valorapesquisa.surveys s
                WHERE s.organization_id=@organizationId AND s.id=@surveyId AND coalesce(s.is_deleted,false)=false
                FOR UPDATE
                """, new { organizationId, surveyId }, u.Transaction, cancellationToken: ct));

            if (current.Id == Guid.Empty)
                throw new KeyNotFoundException("Diagnóstico não encontrado.");

            if (dto.RowVersion <= 0)
                throw new ArgumentException("A versão do registro (rowVersion) é obrigatória e deve ser maior que zero.", nameof(dto.RowVersion));

            if (current.RowVersion != dto.RowVersion)
                throw new DiagnosticConcurrencyException("O diagnóstico foi modificado por outro usuário ou sessão. Recarregue a página para obter a versão mais recente dos parâmetros.");

            if (dto.ResponsibleUserId.HasValue) {
                var userExists = await u.Connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                    "SELECT EXISTS(SELECT 1 FROM valorapesquisa.users WHERE id=@userId AND organization_id=@organizationId AND status='active' AND coalesce(is_deleted,false)=false)",
                    new { userId = dto.ResponsibleUserId.Value, organizationId }, u.Transaction, cancellationToken: ct));
                if (!userExists)
                    throw new ArgumentException("O responsável designado deve ser um usuário ativo da organização.", nameof(dto.ResponsibleUserId));
            }

            var targetVersionId = current.FormVersionId;
            if (dto.FormVersionId.HasValue && dto.FormVersionId.Value != current.FormVersionId) {
                if (current.ResponseCount > 0)
                    throw new InvalidOperationException($"Metodologia fixada porque já existem {current.ResponseCount} respostas coletadas neste ciclo.");
                if (current.Status != "draft")
                    throw new InvalidOperationException("A metodologia só pode ser alterada enquanto o diagnóstico estiver em rascunho.");

                var versionValid = await u.Connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                    "SELECT EXISTS(SELECT 1 FROM valorapesquisa.form_versions WHERE id=@versionId AND form_id=@formId AND status='published')",
                    new { versionId = dto.FormVersionId.Value, formId = current.FormId }, u.Transaction, cancellationToken: ct));
                if (!versionValid)
                    throw new ArgumentException("A versão metodológica selecionada não é válida para este formulário ou não está publicada.", nameof(dto.FormVersionId));

                targetVersionId = dto.FormVersionId.Value;
            }

            var cleanTitle = dto.Title.Trim();
            var cleanDesc = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            var cleanAudience = string.IsNullOrWhiteSpace(dto.TargetAudience) ? null : dto.TargetAudience.Trim();

            var affectedSurveys = await u.Connection.ExecuteAsync(new CommandDefinition(
                """
                UPDATE valorapesquisa.surveys
                SET title = @cleanTitle,
                    description = @cleanDesc,
                    target_audience = @cleanAudience,
                    responsible_user_id = @responsibleUserId,
                    form_version_id = @targetVersionId,
                    starts_at = @startsAt,
                    expires_at = @expiresAt,
                    row_version = coalesce(row_version, 1) + 1,
                    updated_at = now()
                WHERE id = @surveyId AND organization_id = @organizationId AND coalesce(row_version, 1) = @expectedRowVersion;
                """, new {
                    surveyId, organizationId, cleanTitle, cleanDesc, cleanAudience,
                    responsibleUserId = dto.ResponsibleUserId, targetVersionId,
                    startsAt = dto.StartsAt, expiresAt = dto.ExpiresAt,
                    expectedRowVersion = dto.RowVersion
                }, u.Transaction, cancellationToken: ct));

            if (affectedSurveys == 0)
                throw new DiagnosticConcurrencyException("O diagnóstico foi modificado por outro usuário ou sessão durante a gravação. Recarregue a página.");

            await u.Connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO valorapesquisa.diagnostic_cycles
                    (id, organization_id, survey_id, form_id, title, description, target_audience, responsible_user_id, collection_starts_at, collection_ends_at, row_version, updated_at)
                VALUES
                    (gen_random_uuid(), @organizationId, @surveyId, @formId, @cleanTitle, @cleanDesc, @cleanAudience, @responsibleUserId, @startsAt, @expiresAt, coalesce(@rowVersion, 1) + 1, now())
                ON CONFLICT (organization_id, survey_id) WHERE deleted_at IS NULL
                DO UPDATE SET
                    title = @cleanTitle,
                    description = @cleanDesc,
                    target_audience = @cleanAudience,
                    responsible_user_id = @responsibleUserId,
                    collection_starts_at = @startsAt,
                    collection_ends_at = @expiresAt,
                    row_version = coalesce(valorapesquisa.diagnostic_cycles.row_version, 1) + 1,
                    updated_at = now();
                """, new {
                    organizationId, surveyId, formId = current.FormId, cleanTitle, cleanDesc, cleanAudience,
                    responsibleUserId = dto.ResponsibleUserId, startsAt = dto.StartsAt, expiresAt = dto.ExpiresAt,
                    rowVersion = current.RowVersion
                }, u.Transaction, cancellationToken: ct));

            await Audit(u, organizationId, surveyId, userId, "diagnostic.preparation.updated", ct);
            await u.CommitAsync();

            var updatedCycle = await GetCycleAsync(organizationId, surveyId, ct);
            return updatedCycle ?? throw new InvalidOperationException("Falha ao recuperar diagnóstico atualizado.");
        }
        catch {
            await u.RollbackAsync();
            throw;
        }
    }

    private static async Task<Guid> EnsureCycle(System.Data.IDbConnection c, System.Data.IDbTransaction tx, Guid o, DiagnosticCycleDto d, Guid user, CancellationToken ct) => await c.ExecuteScalarAsync<Guid>(new CommandDefinition("INSERT INTO valorapesquisa.diagnostic_cycles(organization_id,survey_id,form_id,title,description,status,methodology_version,created_by) VALUES(@o,@survey,@form,@Title,@Description,@Status,@MethodologyVersion,@user) ON CONFLICT(organization_id,survey_id) WHERE deleted_at IS NULL DO UPDATE SET updated_at=now() RETURNING id", new { o, survey = d.SurveyId, form = d.FormId, d.Title, d.Description, d.Status, d.MethodologyVersion, user }, tx, cancellationToken: ct));
    private static async Task Audit(Valora.Application.Contracts.IUnitOfWork u, Guid o, Guid cycle, Guid user, string code, CancellationToken ct) => await u.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.platform_governance_events(organization_id,cycle_id,code,status,data) VALUES(@o,@cycle,@code,'completed',jsonb_build_object('userId',@user))", new { o, cycle, user, code }, u.Transaction, cancellationToken: ct));
    private sealed record Row(Guid Id, string? Code, string Status, string DataJson, DateTime CreatedAt, DateTime UpdatedAt);
}
