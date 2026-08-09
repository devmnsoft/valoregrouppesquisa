using System.Text.Json;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.OrganizationalIntelligence;

namespace Valora.Infrastructure.Repositories;

public sealed class OrganizationalIntelligenceRepository(IDbConnectionFactory connections, IDbTransactionFactory transactions) : IOrganizationalIntelligenceRepository
{
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

    private static async Task<IReadOnlyList<OrganizationalInsightDto>> InsightsAsync(System.Data.IDbConnection c, Guid runId, CancellationToken ct) =>
        (await c.QueryAsync<OrganizationalInsightDto>(new CommandDefinition("SELECT id,run_id RunId,dimension,observation,evidence,correlation,probable_cause ProbableCause,impact,priority,evolution_plan EvolutionPlan,created_at CreatedAt FROM valorapesquisa.organizational_intelligence_insights WHERE run_id=@runId ORDER BY created_at", new { runId }, cancellationToken: ct))).ToList();
    private static OrganizationalIntelligenceRunDto Map(RunRow x, IReadOnlyList<OrganizationalInsightDto> insights) => new(x.Id, x.OrganizationId, x.MaturityIndex, x.CultureTrustIndex, x.GovernanceExecutionIndex, x.StructuralGap, x.StrongestDimension, x.WeakestDimension, x.EvidenceCount, x.ConfidenceLevel, x.Warning, JsonSerializer.Deserialize<List<DimensionHeatmapDto>>(x.Heatmap) ?? [], insights, x.CreatedAt);
    private sealed record EvidenceCounts(int Responses, int ScoredResults, int Surveys, int ActionPlans);
    private sealed record RunRow(Guid Id, Guid OrganizationId, decimal MaturityIndex, decimal CultureTrustIndex, decimal GovernanceExecutionIndex, decimal StructuralGap, string StrongestDimension, string WeakestDimension, int EvidenceCount, string ConfidenceLevel, string? Warning, string Heatmap, DateTime CreatedAt);
}
