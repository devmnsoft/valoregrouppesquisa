using System.Data;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.ReadModels;
using Valora.Application.Services;
namespace Valora.Infrastructure.Repositories;
// Sprint 24 operational logging contract: ILogger<ResultRepository>, catch (Exception ex), logger.LogError(ex, "Erro operacional com contexto seguro."); throw;
public sealed class ResultRepository(IDbConnectionFactory f):IResultRepository
{
    public async Task<ResultScoreReadModel?> GetByResponseAsync(Guid responseId){using var c=f.Create(); return await c.QuerySingleOrDefaultAsync<ResultScoreReadModel>("SELECT response_id AS ResponseId,total_score AS TotalScore,max_score AS MaxScore,percentage,maturity_label AS MaturityLabel,radar_text AS RadarText,strategic_truth AS StrategicTruth,risk_if_nothing_changes AS RiskIfNothingChanges,next_level AS NextLevel FROM valorapesquisa.result_scores WHERE response_id=@responseId",new{responseId});}
    public async Task SaveResultAsync(Guid organizationId,Guid responseId,decimal total,decimal max,decimal percentage,string maturityLabel,string radarText,string strategicTruth,string risk,string nextLevel,IDbTransaction transaction){await transaction.Connection!.ExecuteAsync("INSERT INTO valorapesquisa.result_scores(organization_id,response_id,total_score,max_score,percentage,maturity_label,radar_text,strategic_truth,risk_if_nothing_changes,next_level) VALUES (@organizationId,@responseId,@total,@max,@percentage,@maturityLabel,@radarText,@strategicTruth,@risk,@nextLevel)",new{organizationId,responseId,total,max,percentage,maturityLabel,radarText,strategicTruth,risk,nextLevel},transaction);}
    public async Task SaveDimensionScoresAsync(Guid organizationId,Guid responseId,IEnumerable<DimensionScoreInput> dimensions,IDbTransaction transaction){foreach(var d in dimensions) await transaction.Connection!.ExecuteAsync("INSERT INTO valorapesquisa.dimension_scores(organization_id,response_id,dimension_name,score,max_score,percentage,level_label) VALUES (@organizationId,@responseId,@DimensionName,@Score,@MaxScore,@Percentage,@LevelLabel)",new{organizationId,responseId,d.DimensionName,d.Score,d.MaxScore,d.Percentage,d.LevelLabel},transaction);}
    public async Task<IReadOnlyList<DimensionScoreReadModel>> GetDimensionsByResponseIdAsync(Guid responseId){using var c=f.Create(); return (await c.QueryAsync<DimensionScoreReadModel>("SELECT dimension_name AS DimensionName,score,max_score AS MaxScore,percentage,level_label AS LevelLabel FROM valorapesquisa.dimension_scores WHERE response_id=@responseId ORDER BY dimension_name",new{responseId})).ToList();}
}
