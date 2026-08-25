using Dapper;
using Valora.Application.Contracts;
using Valora.Application.Heatmap;

namespace Valora.Infrastructure.Repositories;

public sealed class HeatmapRepository(IDbConnectionFactory connections) : IHeatmapRepository
{
    public async Task<IReadOnlyList<HeatmapSnapshotDto>> ListAsync(Guid organizationId, CancellationToken ct)
    {
        const string sql = "SELECT id FROM valorapesquisa.heatmap_snapshots WHERE organization_id=@organizationId AND deleted_at IS NULL ORDER BY generated_at DESC LIMIT 50";
        using var c=connections.Create(); var ids=await c.QueryAsync<Guid>(new CommandDefinition(sql,new{organizationId},cancellationToken:ct));
        var result=new List<HeatmapSnapshotDto>(); foreach(var id in ids){var item=await GetAsync(organizationId,id,ct);if(item is not null)result.Add(item);} return result;
    }

    public async Task<HeatmapSnapshotDto?> GetAsync(Guid organizationId, Guid id, CancellationToken ct)
    {
        const string head="""
        SELECT id,organization_id OrganizationId,diagnostic_id DiagnosticId,result_id ResultId,title,snapshot_type SnapshotType,status,
          generated_at GeneratedAt,evidence_summary EvidenceSummary,ai_summary AiSummary
        FROM valorapesquisa.heatmap_snapshots WHERE id=@id AND organization_id=@organizationId AND deleted_at IS NULL
        """;
        const string cells="""
        SELECT id,dimension,index_code IndexCode,area_name AreaName,unit_name UnitName,leadership_name LeadershipName,score,level,
          risk_level RiskLevel,trend,response_count ResponseCount,evidence_summary EvidenceSummary,recommendation,
          (response_count<@minimumSample) InsufficientSample
        FROM valorapesquisa.heatmap_cells WHERE heatmap_snapshot_id=@id AND organization_id=@organizationId AND deleted_at IS NULL ORDER BY score NULLS FIRST,dimension
        """;
        using var c=connections.Create(); var row=await c.QuerySingleOrDefaultAsync<Row>(new CommandDefinition(head,new{id,organizationId},cancellationToken:ct));
        if(row is null)return null; var values=(await c.QueryAsync<HeatmapCellDto>(new CommandDefinition(cells,new{id,organizationId,minimumSample=HeatmapCalculationService.MinimumSample},cancellationToken:ct))).ToList();
        return new(row.Id,row.OrganizationId,row.DiagnosticId,row.ResultId,row.Title,row.SnapshotType,row.Status,row.GeneratedAt,row.EvidenceSummary,row.AiSummary,values);
    }

    public async Task<HeatmapSnapshotDto> GenerateAsync(Guid organizationId, Guid? userId, HeatmapFilter f, CancellationToken ct)
    {
        const string sql="""
        WITH source AS (
          SELECT d.code dimension,round(avg(ds.score::numeric/nullif(ds.max_score,0))*100,2) score,count(DISTINCT r.id)::int response_count
          FROM valorapesquisa.responses r JOIN valorapesquisa.result_scores rs ON rs.response_id=r.id
          JOIN valorapesquisa.dimension_scores ds ON ds.result_score_id=rs.id JOIN valorapesquisa.dimensions d ON d.id=ds.dimension_id
          WHERE r.organization_id=@organizationId AND r.survey_id=@diagnosticId
            AND (@periodStart IS NULL OR r.created_at>=@periodStart) AND (@periodEnd IS NULL OR r.created_at<@periodEnd)
          GROUP BY d.code
        ), snapshot AS (
          INSERT INTO valorapesquisa.heatmap_snapshots(organization_id,diagnostic_id,result_id,title,snapshot_type,status,generated_by_user_id,
            generated_at,filters_json,evidence_summary,ai_summary,metadata_json)
          SELECT @organizationId,@diagnosticId,@resultId,'Heatmap organizacional',@viewBy,'generated',@userId,now(),
            jsonb_build_object('viewBy',@viewBy,'area',@area,'unit',@unit,'leadership',@leadership,'indexCode',@indexCode,'periodStart',@periodStart,'periodEnd',@periodEnd),
            coalesce(sum(response_count),0)||' evidências agregadas por dimensão.',
            'Leitura descritiva: sinais orientam investigação e não determinam causa ou culpa.',jsonb_build_object('source','dimension_scores','method','deterministic') FROM source RETURNING id
        )
        , inserted_cells AS (INSERT INTO valorapesquisa.heatmap_cells(organization_id,heatmap_snapshot_id,dimension,index_code,area_name,unit_name,leadership_name,
          score,level,risk_level,trend,response_count,evidence_summary,recommendation,metadata_json)
        SELECT @organizationId,snapshot.id,source.dimension,@indexCode,@area,@unit,@leadership,source.score,
          CASE WHEN response_count<@minimumSample THEN 'amostra insuficiente' WHEN score>=80 THEN 'excelente' WHEN score>=65 THEN 'saudável' WHEN score>=50 THEN 'em atenção' WHEN score>=35 THEN 'crítico' ELSE 'muito crítico' END,
          CASE WHEN response_count<@minimumSample THEN 'indeterminado' WHEN score>=80 THEN 'baixo' WHEN score>=65 THEN 'moderado' WHEN score>=50 THEN 'atenção' WHEN score>=35 THEN 'alto' ELSE 'muito alto' END,
          'sem série comparável',response_count,response_count||' respostas agregadas; nenhuma pessoa é identificada.',
          CASE WHEN response_count<@minimumSample THEN 'Ampliar a amostra antes de interpretar.' ELSE 'Validar o sinal com evidências qualitativas e contexto organizacional.' END,'{}'::jsonb
        FROM snapshot CROSS JOIN source RETURNING heatmap_snapshot_id)
        SELECT id FROM snapshot
        """;
        using var c=connections.Create(); var id=await c.ExecuteScalarAsync<Guid?>(new CommandDefinition(sql,new{organizationId,userId,diagnosticId=f.DiagnosticId,resultId=f.ResultId,viewBy=f.ViewBy,area=f.Area,unit=f.Unit,leadership=f.Leadership,indexCode=f.IndexCode,periodStart=f.PeriodStart,periodEnd=f.PeriodEnd,minimumSample=HeatmapCalculationService.MinimumSample},cancellationToken:ct));
        if(!id.HasValue)
        {
            const string empty="""INSERT INTO valorapesquisa.heatmap_snapshots(organization_id,diagnostic_id,result_id,title,snapshot_type,status,generated_by_user_id,filters_json,evidence_summary,ai_summary) VALUES(@organizationId,@diagnosticId,@resultId,'Heatmap organizacional',@viewBy,'insufficient_data',@userId,'{}','Nenhuma evidência pontuada foi encontrada.','Não há base suficiente para interpretação.') RETURNING id""";
            id=await c.ExecuteScalarAsync<Guid>(new CommandDefinition(empty,new{organizationId,userId,diagnosticId=f.DiagnosticId,resultId=f.ResultId,viewBy=f.ViewBy},cancellationToken:ct));
        }
        return (await GetAsync(organizationId,id.Value,ct))!;
    }
    private sealed class Row { public Guid Id{get;set;} public Guid OrganizationId{get;set;} public Guid DiagnosticId{get;set;} public Guid? ResultId{get;set;} public string Title{get;set;}=""; public string SnapshotType{get;set;}=""; public string Status{get;set;}=""; public DateTime GeneratedAt{get;set;} public string EvidenceSummary{get;set;}=""; public string? AiSummary{get;set;} }
}
