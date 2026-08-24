using System.Text.Json;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.OrganizationalIntelligence;

namespace Valora.Infrastructure.Repositories;

public sealed class BenchmarkRepository(IDbConnectionFactory connections) : IBenchmarkRepository
{
    private const string Select = """
      SELECT id,organization_id OrganizationId,survey_id SurveyId,result_id ResultId,snapshot_type SnapshotType,
       maturity_score MaturityScore,maturity_level MaturityLevel,total_responses TotalResponses,
       dimensions_json::text DimensionsJson,evidence_summary EvidenceSummary,generated_at GeneratedAt,metadata_json::text MetadataJson
      FROM valorapesquisa.benchmark_snapshots WHERE organization_id=@organizationId AND deleted_at IS NULL
      """;

    public async Task<BenchmarkSettings> SettingsAsync(Guid organizationId, CancellationToken ct)
    {
        const string sql = "SELECT minimum_organizations,minimum_responses,external_enabled,require_anonymization FROM valorapesquisa.benchmark_settings WHERE organization_id=@organizationId";
        using var c = connections.Create();
        var row = await c.QuerySingleOrDefaultAsync<SettingsRow>(new CommandDefinition(sql, new { organizationId }, cancellationToken: ct));
        return row is null ? new() : new(row.MinimumOrganizations,row.MinimumResponses,row.ExternalEnabled,row.RequireAnonymization);
    }
    public async Task<IReadOnlyList<BenchmarkSnapshotDto>> ListAsync(Guid organizationId, CancellationToken ct)
    {
        using var c = connections.Create();
        var rows = await c.QueryAsync<Row>(new CommandDefinition(Select + " ORDER BY generated_at DESC LIMIT 100", new { organizationId }, cancellationToken: ct));
        return rows.Select(Map).ToList();
    }
    public async Task<BenchmarkSnapshotDto?> GetAsync(Guid organizationId, Guid id, CancellationToken ct)
    {
        using var c = connections.Create();
        return Map(await c.QuerySingleOrDefaultAsync<Row>(new CommandDefinition(Select + " AND id=@id", new { organizationId, id }, cancellationToken: ct)));
    }
    public async Task<BenchmarkSnapshotDto> GenerateAsync(Guid organizationId, GenerateBenchmarkRequest request, CancellationToken ct)
    {
        const string sql = """
        WITH dimensions AS (
          SELECT d.code,d.name,round(avg(ds.score::numeric/nullif(ds.max_score,0))*100,2) score,count(*)::int evidence_count
          FROM valorapesquisa.responses r JOIN valorapesquisa.result_scores rs ON rs.response_id=r.id
          JOIN valorapesquisa.dimension_scores ds ON ds.result_score_id=rs.id JOIN valorapesquisa.dimensions d ON d.id=ds.dimension_id
          WHERE r.organization_id=@organizationId AND r.survey_id=@surveyId GROUP BY d.code,d.name
        ), aggregate AS (SELECT coalesce(round(avg(score),2),0) score,coalesce(sum(evidence_count),0)::int evidence_count,
          coalesce(jsonb_agg(jsonb_build_object('code',code,'name',name,'score',score,'referenceScore',null,'delta',null,'trend','baseline','evidenceCount',evidence_count)),'[]') dimensions FROM dimensions),
        inserted AS (INSERT INTO valorapesquisa.benchmark_snapshots(organization_id,survey_id,result_id,snapshot_type,maturity_score,maturity_level,total_responses,dimensions_json,evidence_summary,metadata_json)
          SELECT @organizationId,@surveyId,@resultId,@snapshotType,score,CASE WHEN score>=80 THEN 'Avançada' WHEN score>=60 THEN 'Estruturada' WHEN score>=40 THEN 'Em desenvolvimento' ELSE 'Inicial' END,
          (SELECT count(*) FROM valorapesquisa.responses WHERE organization_id=@organizationId AND survey_id=@surveyId),dimensions,
          evidence_count||' evidências agregadas; nenhuma resposta individual é exposta.',jsonb_build_object('source','result_scores','method','deterministic') FROM aggregate RETURNING id)
        SELECT id FROM inserted
        """;
        using var c = connections.Create();
        var id = await c.ExecuteScalarAsync<Guid>(new CommandDefinition(sql, new { organizationId, surveyId=request.SurveyId, resultId=request.ResultId, snapshotType=request.SnapshotType }, cancellationToken: ct));
        return (await GetAsync(organizationId, id, ct))!;
    }
    public async Task<BenchmarkComparisonDto> CompareAsync(Guid organizationId, CompareBenchmarkRequest request, CancellationToken ct)
    {
        const string sql = """
        WITH base AS (SELECT * FROM valorapesquisa.benchmark_snapshots WHERE id=@baseId AND organization_id=@organizationId AND deleted_at IS NULL),
        previous AS (SELECT * FROM valorapesquisa.benchmark_snapshots WHERE organization_id=@organizationId AND deleted_at IS NULL AND
          id=coalesce(@comparedId,(SELECT id FROM valorapesquisa.benchmark_snapshots WHERE organization_id=@organizationId AND id<>@baseId AND deleted_at IS NULL ORDER BY generated_at DESC LIMIT 1))),
        inserted AS (INSERT INTO valorapesquisa.benchmark_comparisons(organization_id,base_snapshot_id,compared_snapshot_id,comparison_type,score_delta,maturity_delta,strengths_json,risks_json,opportunities_json,recommendations_json,metadata_json)
          SELECT @organizationId,b.id,p.id,@comparisonType,CASE WHEN p.id IS NULL THEN NULL ELSE b.maturity_score-p.maturity_score END,
          CASE WHEN p.id IS NULL THEN 'Sem ciclo anterior' WHEN b.maturity_score>p.maturity_score THEN 'Evolução' WHEN b.maturity_score<p.maturity_score THEN 'Queda' ELSE 'Estabilidade' END,
          '[]','[]','[]','[]',jsonb_build_object('limitation',CASE WHEN p.id IS NULL THEN 'Não há diagnóstico anterior comparável.' ELSE '' END) FROM base b LEFT JOIN previous p ON true RETURNING *)
        SELECT id,organization_id OrganizationId,base_snapshot_id BaseSnapshotId,compared_snapshot_id ComparedSnapshotId,comparison_type ComparisonType,
          score_delta ScoreDelta,maturity_delta MaturityDelta,strengths_json::text StrengthsJson,risks_json::text RisksJson,opportunities_json::text OpportunitiesJson,
          recommendations_json::text RecommendationsJson,metadata_json->>'limitation' Limitation,created_at CreatedAt FROM inserted
        """;
        using var c = connections.Create();
        var row = await c.QuerySingleOrDefaultAsync<ComparisonRow>(new CommandDefinition(sql, new { organizationId, baseId=request.BaseSnapshotId, comparedId=request.ComparedSnapshotId, comparisonType=request.ComparisonType }, cancellationToken: ct))
            ?? throw new KeyNotFoundException("Snapshot não encontrado nesta organização.");
        return new(row.Id,row.OrganizationId,row.BaseSnapshotId,row.ComparedSnapshotId,row.ComparisonType,row.ScoreDelta,row.MaturityDelta,Strings(row.StrengthsJson),Strings(row.RisksJson),Strings(row.OpportunitiesJson),Strings(row.RecommendationsJson),row.Limitation ?? "",row.CreatedAt);
    }
    public async Task SaveSettingsAsync(Guid organizationId, BenchmarkSettings s, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO valorapesquisa.benchmark_settings(organization_id,minimum_organizations,minimum_responses,external_enabled,require_anonymization)
            VALUES(@organizationId,@MinimumOrganizations,@MinimumResponses,@ExternalEnabled,@RequireAnonymization)
            ON CONFLICT(organization_id) DO UPDATE SET
              minimum_organizations=excluded.minimum_organizations,
              minimum_responses=excluded.minimum_responses,
              external_enabled=excluded.external_enabled,
              require_anonymization=excluded.require_anonymization,
              updated_at=now()
            """;
        using var c=connections.Create(); await c.ExecuteAsync(new CommandDefinition(sql,new {organizationId,s.MinimumOrganizations,s.MinimumResponses,s.ExternalEnabled,s.RequireAnonymization},cancellationToken:ct));
    }
    private static BenchmarkSnapshotDto? Map(Row? r) => r is null ? null : new(r.Id,r.OrganizationId,r.SurveyId,r.ResultId,r.SnapshotType,r.MaturityScore,r.MaturityLevel,r.TotalResponses,JsonSerializer.Deserialize<List<BenchmarkDimension>>(r.DimensionsJson,new JsonSerializerOptions{PropertyNameCaseInsensitive=true}) ?? [],r.EvidenceSummary,r.GeneratedAt,JsonSerializer.Deserialize<JsonElement>(r.MetadataJson));
    private static IReadOnlyList<string> Strings(string json) => JsonSerializer.Deserialize<List<string>>(json) ?? [];
    private sealed class Row { public Guid Id {get;set;} public Guid OrganizationId {get;set;} public Guid? SurveyId {get;set;} public Guid? ResultId {get;set;} public string SnapshotType {get;set;}=""; public decimal MaturityScore {get;set;} public string MaturityLevel {get;set;}=""; public int TotalResponses {get;set;} public string DimensionsJson {get;set;}="[]"; public string EvidenceSummary {get;set;}=""; public DateTime GeneratedAt {get;set;} public string MetadataJson {get;set;}="{}"; }
    private sealed class ComparisonRow { public Guid Id {get;set;} public Guid OrganizationId {get;set;} public Guid BaseSnapshotId {get;set;} public Guid? ComparedSnapshotId {get;set;} public string ComparisonType {get;set;}=""; public decimal? ScoreDelta {get;set;} public string MaturityDelta {get;set;}=""; public string StrengthsJson {get;set;}="[]"; public string RisksJson {get;set;}="[]"; public string OpportunitiesJson {get;set;}="[]"; public string RecommendationsJson {get;set;}="[]"; public string? Limitation {get;set;} public DateTime CreatedAt {get;set;} }
    private sealed class SettingsRow { public int MinimumOrganizations {get;set;} public int MinimumResponses {get;set;} public bool ExternalEnabled {get;set;} public bool RequireAnonymization {get;set;} }
}
