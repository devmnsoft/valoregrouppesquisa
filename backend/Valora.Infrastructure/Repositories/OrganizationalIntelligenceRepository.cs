using System.Text.Json;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.OrganizationalIntelligence;

namespace Valora.Infrastructure.Repositories;

public sealed class OrganizationalIntelligenceRepository(IDbConnectionFactory connections, IDbTransactionFactory transactions) : IOrganizationalIntelligenceRepository
{
    private static readonly IReadOnlyDictionary<string, string> ModuleTables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["metrics"] = "metric_values", ["indices"] = "index_values", ["inferences"] = "inference_results",
        ["insights"] = "insights", ["radar"] = "radar_snapshots", ["heatmap-snapshots"] = "heatmap_snapshots",
        ["benchmark"] = "benchmark_runs", ["evolution-cycles"] = "evolution_cycles", ["journey-events"] = "journey_events",
        ["executive-reports"] = "executive_reports", ["one-on-one"] = "one_on_one_sessions",
        ["integrations"] = "integration_connectors", ["platform-governance"] = "platform_governance_events"
    };

    public async Task<IReadOnlyList<EvidenceItemDto>> ListEvidenceItemsAsync(Guid organizationId, CancellationToken ct)
    {
        const string sql = """
            SELECT id,survey_id SurveyId,response_id ResponseId,form_id FormId,question_id QuestionId,
              concept_code ConceptCode,capability_code CapabilityCode,dimension_code DimensionCode,
              metric_code MetricCode,index_code IndexCode,
              evidence_type EvidenceType,source_type SourceType,normalized_value NormalizedValue,weight,
              confidence_weight ConfidenceWeight,text_excerpt TextExcerpt,
              coalesce(metadata_json->>'mappingStatus',CASE WHEN metric_code IS NOT NULL AND index_code IS NOT NULL AND concept_code<>'unmapped' THEN 'mapped' ELSE 'pending' END) MappingStatus,
              created_at CreatedAt
            FROM valorapesquisa.evidence_items
            WHERE organization_id=@organizationId AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 250
            """;
        using var c = connections.Create();
        return (await c.QueryAsync<EvidenceItemDto>(new CommandDefinition(sql, new { organizationId }, cancellationToken: ct))).ToList();
    }

    public async Task<IReadOnlyList<IntelligenceModuleRecordDto>> ListModuleRecordsAsync(Guid organizationId, string module, CancellationToken ct)
    {
        if (!ModuleTables.TryGetValue(module, out var table)) throw new ArgumentException("Módulo de inteligência inválido.", nameof(module));
        var sql = $"SELECT id,code,status,data::text DataJson,methodology_version MethodologyVersion,version,created_at CreatedAt,updated_at UpdatedAt FROM valorapesquisa.{table} WHERE organization_id=@organizationId AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 100";
        using var c = connections.Create();
        var rows = await c.QueryAsync<ModuleRow>(new CommandDefinition(sql, new { organizationId }, cancellationToken: ct));
        return rows.Select(x => new IntelligenceModuleRecordDto(x.Id, x.Code, x.Status,
            JsonSerializer.Deserialize<JsonElement>(x.DataJson), x.MethodologyVersion, x.Version, x.CreatedAt, x.UpdatedAt)).ToList();
    }

    private sealed record ModuleRow(Guid Id, string? Code, string Status, string DataJson, int MethodologyVersion, int Version, DateTime CreatedAt, DateTime UpdatedAt);
    public async Task<EvidenceSummaryDto> GetEvidenceAsync(Guid organizationId, CancellationToken ct)
    {
        const string dimensions = """
            SELECT d.id DimensionId,d.code Code,d.name Name,
              round(avg(ds.score::numeric/nullif(ds.max_score,0))*100,2) Score,count(*)::int EvidenceCount
            FROM valorapesquisa.dimension_scores ds
            JOIN valorapesquisa.result_scores rs ON rs.id=ds.result_score_id
            JOIN valorapesquisa.responses r ON r.id=rs.response_id
            JOIN valorapesquisa.dimensions d ON d.id=ds.dimension_id
            WHERE r.organization_id=@organizationId GROUP BY d.id,d.code,d.name ORDER BY d.display_order
            """;
        const string counts = """
            SELECT (SELECT count(*) FROM valorapesquisa.responses WHERE organization_id=@organizationId)::int Responses,
              (SELECT count(DISTINCT res.id) FROM valorapesquisa.results res
                 JOIN valorapesquisa.result_scores rs ON rs.id=res.result_score_id
                 JOIN valorapesquisa.response_answers ra ON ra.response_id=rs.response_id
                WHERE res.organization_id=@organizationId)::int ScoredResults,
              (SELECT count(*) FROM valorapesquisa.surveys WHERE organization_id=@organizationId AND deleted_at IS NULL)::int Surveys,
              (SELECT count(*) FROM valorapesquisa.action_plans WHERE organization_id=@organizationId)::int ActionPlans
            """;
        using var c = connections.Create();
        var count = await c.QuerySingleAsync<EvidenceCounts>(new CommandDefinition(counts, new { organizationId }, cancellationToken: ct));
        var heatmap = (await c.QueryAsync<DimensionHeatmapDto>(new CommandDefinition(dimensions, new { organizationId }, cancellationToken: ct))).ToList();
        return new(count.Responses, count.ScoredResults, count.Surveys, count.ActionPlans, heatmap);
    }

    public async Task<OrganizationalIntelligenceDashboardDto> GetDashboardAsync(Guid organizationId, CancellationToken ct)
    {
        var runs = await ListRunsAsync(organizationId, ct);
        return new(runs.FirstOrDefault(), await GetEvidenceAsync(organizationId, ct), await ListJourneyAsync(organizationId, ct), await ListIndicatorsAsync(ct));
    }

    public async Task<IReadOnlyList<OrganizationalIntelligenceRunDto>> ListRunsAsync(Guid organizationId, CancellationToken ct)
    {
        const string sql = "SELECT id,organization_id OrganizationId,maturity_index MaturityIndex,culture_trust_index CultureTrustIndex,governance_execution_index GovernanceExecutionIndex,structural_gap StructuralGap,strongest_dimension StrongestDimension,weakest_dimension WeakestDimension,evidence_count EvidenceCount,confidence_level ConfidenceLevel,warning,heatmap::text Heatmap,created_at CreatedAt FROM valorapesquisa.organizational_intelligence_runs WHERE organization_id=@organizationId ORDER BY created_at DESC";
        using var c = connections.Create();
        var rows = await c.QueryAsync<RunRow>(new CommandDefinition(sql, new { organizationId }, cancellationToken: ct));
        var result = new List<OrganizationalIntelligenceRunDto>();
        foreach (var row in rows) result.Add(Map(row, await InsightsAsync(c, row.Id, ct)));
        return result;
    }

    public async Task<OrganizationalIntelligenceRunDto?> GetRunAsync(Guid organizationId, Guid id, CancellationToken ct) =>
        (await ListRunsAsync(organizationId, ct)).FirstOrDefault(x => x.Id == id);

    public async Task SaveAnalysisAsync(OrganizationalIntelligenceRunDto x, CancellationToken ct)
    {
        const string sql = "INSERT INTO valorapesquisa.organizational_intelligence_runs(id,organization_id,maturity_index,culture_trust_index,governance_execution_index,structural_gap,strongest_dimension,weakest_dimension,evidence_count,confidence_level,warning,heatmap,created_at) VALUES(@Id,@OrganizationId,@MaturityIndex,@CultureTrustIndex,@GovernanceExecutionIndex,@StructuralGap,@StrongestDimension,@WeakestDimension,@EvidenceCount,@ConfidenceLevel,@Warning,CAST(@Heatmap AS jsonb),@CreatedAt)";
        const string insightSql = "INSERT INTO valorapesquisa.organizational_intelligence_insights(id,run_id,dimension,observation,evidence,correlation,probable_cause,impact,priority,evolution_plan,created_at) VALUES(@Id,@RunId,@Dimension,@Observation,@Evidence,@Correlation,@ProbableCause,@Impact,@Priority,@EvolutionPlan,@CreatedAt)";
        await using var unit = await transactions.BeginAsync(ct);
        try
        {
            await unit.Connection.ExecuteAsync(new CommandDefinition(sql, new { x.Id, x.OrganizationId, x.MaturityIndex, x.CultureTrustIndex, x.GovernanceExecutionIndex, x.StructuralGap, x.StrongestDimension, x.WeakestDimension, x.EvidenceCount, x.ConfidenceLevel, x.Warning, Heatmap = JsonSerializer.Serialize(x.Heatmap), x.CreatedAt }, unit.Transaction, cancellationToken: ct));
            await unit.Connection.ExecuteAsync(new CommandDefinition(insightSql, x.Insights, unit.Transaction, cancellationToken: ct));
            await unit.CommitAsync();
        }
        catch
        {
            await unit.RollbackAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<OrganizationalJourneyEventDto>> ListJourneyAsync(Guid organizationId, CancellationToken ct)
    { using var c = connections.Create(); return (await c.QueryAsync<OrganizationalJourneyEventDto>(new CommandDefinition("SELECT id,organization_id OrganizationId,title,description,event_type EventType,occurred_at OccurredAt,created_by CreatedBy,created_at CreatedAt FROM valorapesquisa.organizational_journey_events WHERE organization_id=@organizationId ORDER BY occurred_at DESC", new { organizationId }, cancellationToken: ct))).ToList(); }
    public async Task<OrganizationalJourneyEventDto> CreateJourneyEventAsync(OrganizationalJourneyEventDto item, CancellationToken ct)
    { using var c = connections.Create(); await c.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.organizational_journey_events(id,organization_id,title,description,event_type,occurred_at,created_by,created_at) VALUES(@Id,@OrganizationId,@Title,@Description,@EventType,@OccurredAt,@CreatedBy,@CreatedAt)", item, cancellationToken: ct)); return item; }
    public async Task<IReadOnlyList<ValoraIndicatorDefinitionDto>> ListIndicatorsAsync(CancellationToken ct)
    { using var c = connections.Create(); return (await c.QueryAsync<ValoraIndicatorDefinitionDto>(new CommandDefinition("SELECT id,code,name,description,category,weight,is_active IsActive FROM valorapesquisa.valora_indicator_definitions WHERE is_active ORDER BY category,name", cancellationToken: ct))).ToList(); }

    public async Task<IReadOnlyList<ValoraActionDto>> ListActionsAsync(Guid organizationId, CancellationToken ct)
    { using var c = connections.Create(); return (await c.QueryAsync<ValoraActionDto>(new CommandDefinition("SELECT id,organization_id OrganizationId,code,title,description,evidence_justification EvidenceJustification,capability,priority,owner_name Owner,executive_sponsor ExecutiveSponsor,due_at DueAt,complexity,indicators,expected_result ExpectedResult,completion_criteria CompletionCriteria,status,created_at CreatedAt,updated_at UpdatedAt FROM valorapesquisa.valora_actions WHERE organization_id=@organizationId AND deleted_at IS NULL ORDER BY created_at DESC", new { organizationId }, cancellationToken: ct))).ToList(); }

    public async Task<ValoraActionDto> CreateActionAsync(ValoraActionDto item, Guid userId, CancellationToken ct)
    {
        const string actionSql = "INSERT INTO valorapesquisa.valora_actions(id,organization_id,code,title,description,evidence_justification,capability,priority,owner_name,executive_sponsor,due_at,complexity,indicators,expected_result,completion_criteria,status,created_by,created_at,updated_at) VALUES(@Id,@OrganizationId,@Code,@Title,@Description,@EvidenceJustification,@Capability,@Priority,@Owner,@ExecutiveSponsor,@DueAt,@Complexity,@Indicators,@ExpectedResult,@CompletionCriteria,@Status,@userId,@CreatedAt,@UpdatedAt)";
        const string historySql = "INSERT INTO valorapesquisa.valora_action_history(action_id,status,notes,changed_by) VALUES(@Id,@Status,'Ação criada a partir de evidência organizacional.',@userId)";
        const string journeySql = "INSERT INTO valorapesquisa.organizational_journey_events(organization_id,title,description,event_type,occurred_at,created_by) VALUES(@OrganizationId,'Plano de ação criado',@Title,'action_created',@CreatedAt,@userId)";
        await using var unit = await transactions.BeginAsync(ct);
        try { await unit.Connection.ExecuteAsync(new CommandDefinition(actionSql, new { item.Id,item.OrganizationId,item.Code,item.Title,item.Description,item.EvidenceJustification,item.Capability,item.Priority,item.Owner,item.ExecutiveSponsor,item.DueAt,item.Complexity,item.Indicators,item.ExpectedResult,item.CompletionCriteria,item.Status,userId,item.CreatedAt,item.UpdatedAt }, unit.Transaction, cancellationToken: ct)); await unit.Connection.ExecuteAsync(new CommandDefinition(historySql, new { item.Id,item.Status,userId }, unit.Transaction, cancellationToken: ct)); await unit.Connection.ExecuteAsync(new CommandDefinition(journeySql, new { item.OrganizationId,item.Title,item.CreatedAt,userId }, unit.Transaction, cancellationToken: ct)); await unit.CommitAsync(); return item; }
        catch { await unit.RollbackAsync(); throw; }
    }

    public async Task<ValoraActionDto?> UpdateActionAsync(Guid organizationId, Guid actionId, UpdateValoraActionRequest request, Guid userId, CancellationToken ct)
    {
        const string updateSql = "UPDATE valorapesquisa.valora_actions SET status=@Status,owner_name=coalesce(@Owner,owner_name),executive_sponsor=coalesce(@ExecutiveSponsor,executive_sponsor),due_at=coalesce(@DueAt,due_at),priority=coalesce(@Priority,priority),updated_at=now() WHERE id=@actionId AND organization_id=@organizationId AND deleted_at IS NULL";
        const string historySql = "INSERT INTO valorapesquisa.valora_action_history(action_id,status,notes,changed_by) VALUES(@actionId,@Status,coalesce(@Notes,'Status atualizado.'),@userId)";
        const string journeySql = "INSERT INTO valorapesquisa.organizational_journey_events(organization_id,title,description,event_type,occurred_at,created_by) SELECT organization_id,'Plano de ação atualizado',title,CASE WHEN @Status='completed' THEN 'action_completed' ELSE 'action_updated' END,now(),@userId FROM valorapesquisa.valora_actions WHERE id=@actionId AND organization_id=@organizationId";
        await using var unit = await transactions.BeginAsync(ct);
        try
        {
            var changed = await unit.Connection.ExecuteAsync(new CommandDefinition(updateSql, new { request.Status, request.Owner, request.ExecutiveSponsor, request.DueAt, request.Priority, actionId, organizationId }, unit.Transaction, cancellationToken: ct));
            if (changed == 0) { await unit.RollbackAsync(); return null; }
            await unit.Connection.ExecuteAsync(new CommandDefinition(historySql, new { actionId, request.Status, request.Notes, userId }, unit.Transaction, cancellationToken: ct));
            await unit.Connection.ExecuteAsync(new CommandDefinition(journeySql, new { actionId, organizationId, request.Status, userId }, unit.Transaction, cancellationToken: ct));
            await unit.CommitAsync();
        }
        catch { await unit.RollbackAsync(); throw; }
        return (await ListActionsAsync(organizationId, ct)).FirstOrDefault(x => x.Id == actionId);
    }

    public async Task<IReadOnlyList<ValoraActionHistoryDto>> ListActionHistoryAsync(Guid organizationId, Guid actionId, CancellationToken ct)
    {
        const string sql = "SELECT h.id,h.action_id ActionId,h.status,h.notes,h.changed_by ChangedBy,h.changed_at ChangedAt FROM valorapesquisa.valora_action_history h JOIN valorapesquisa.valora_actions a ON a.id=h.action_id WHERE a.organization_id=@organizationId AND a.id=@actionId ORDER BY h.changed_at DESC";
        using var c = connections.Create();
        return (await c.QueryAsync<ValoraActionHistoryDto>(new CommandDefinition(sql, new { organizationId, actionId }, cancellationToken: ct))).ToList();
    }

    public async Task<bool> DeleteActionAsync(Guid organizationId, Guid actionId, Guid userId, CancellationToken ct)
    {
        const string updateSql = "UPDATE valorapesquisa.valora_actions SET deleted_at=now(),updated_at=now() WHERE id=@actionId AND organization_id=@organizationId AND deleted_at IS NULL";
        const string historySql = "INSERT INTO valorapesquisa.valora_action_history(action_id,status,notes,changed_by) VALUES(@actionId,'cancelled','Ação arquivada pelo usuário.',@userId)";
        const string journeySql = "INSERT INTO valorapesquisa.organizational_journey_events(organization_id,title,description,event_type,occurred_at,created_by) SELECT organization_id,'Plano de ação arquivado',title,'action_archived',now(),@userId FROM valorapesquisa.valora_actions WHERE id=@actionId AND organization_id=@organizationId";
        await using var unit = await transactions.BeginAsync(ct);
        try
        {
            var changed = await unit.Connection.ExecuteAsync(new CommandDefinition(updateSql, new { actionId, organizationId }, unit.Transaction, cancellationToken: ct));
            if (changed == 0) { await unit.RollbackAsync(); return false; }
            await unit.Connection.ExecuteAsync(new CommandDefinition(historySql, new { actionId, userId }, unit.Transaction, cancellationToken: ct));
            await unit.Connection.ExecuteAsync(new CommandDefinition(journeySql, new { actionId, organizationId, userId }, unit.Transaction, cancellationToken: ct));
            await unit.CommitAsync();
            return true;
        }
        catch { await unit.RollbackAsync(); throw; }
    }

    private static async Task<IReadOnlyList<OrganizationalInsightDto>> InsightsAsync(System.Data.IDbConnection c, Guid runId, CancellationToken ct) =>
        (await c.QueryAsync<OrganizationalInsightDto>(new CommandDefinition("SELECT id,run_id RunId,dimension,observation,evidence,correlation,probable_cause ProbableCause,impact,priority,evolution_plan EvolutionPlan,created_at CreatedAt FROM valorapesquisa.organizational_intelligence_insights WHERE run_id=@runId ORDER BY created_at", new { runId }, cancellationToken: ct))).ToList();
    private static OrganizationalIntelligenceRunDto Map(RunRow x, IReadOnlyList<OrganizationalInsightDto> insights) => new(x.Id, x.OrganizationId, x.MaturityIndex, x.CultureTrustIndex, x.GovernanceExecutionIndex, x.StructuralGap, x.StrongestDimension, x.WeakestDimension, x.EvidenceCount, x.ConfidenceLevel, x.Warning, JsonSerializer.Deserialize<List<DimensionHeatmapDto>>(x.Heatmap) ?? [], insights, x.CreatedAt);
    private sealed record EvidenceCounts(int Responses, int ScoredResults, int Surveys, int ActionPlans);
    private sealed record RunRow(Guid Id, Guid OrganizationId, decimal MaturityIndex, decimal CultureTrustIndex, decimal GovernanceExecutionIndex, decimal StructuralGap, string StrongestDimension, string WeakestDimension, int EvidenceCount, string ConfidenceLevel, string? Warning, string Heatmap, DateTime CreatedAt);
}
