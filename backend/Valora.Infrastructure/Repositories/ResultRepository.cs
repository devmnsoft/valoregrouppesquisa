using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.ReadModels;
using Valora.Application.Services;

namespace Valora.Infrastructure.Repositories;

public sealed class ResultRepository(IDbConnectionFactory factory, ILogger<ResultRepository> logger) : IResultRepository
{
    public async Task<ResultScoreReadModel?> GetByResponseAsync(Guid responseId)
    {
        try
        {
            using var connection = factory.Create();
            const string sql = """
                SELECT response_id AS "ResponseId",
                       total_score AS "TotalScore",
                       max_score AS "MaxScore",
                       percentage::numeric AS "Percentage",
                       maturity_label AS "MaturityLabel",
                       radar_text AS "RadarText",
                       strategic_truth AS "StrategicTruth",
                       risk_if_nothing_changes AS "RiskIfNothingChanges",
                       next_level AS "NextLevel"
                  FROM valorapesquisa.result_scores
                 WHERE response_id = @responseId
                """;
            return await connection.QuerySingleOrDefaultAsync<ResultScoreReadModel>(sql, new { responseId });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Erro ao buscar resultado. ResponseId={ResponseId}", responseId);
            throw;
        }
    }

    public async Task SaveResultAsync(Guid organizationId, Guid responseId, decimal total, decimal max,
        decimal percentage, string maturityLabel, string radarText, string strategicTruth, string risk,
        string nextLevel, IDbTransaction transaction)
    {
        try
        {
            const string sql = """
                INSERT INTO valorapesquisa.result_scores
                    (organization_id, response_id, total_score, max_score, percentage, maturity_label,
                     radar_text, strategic_truth, risk_if_nothing_changes, next_level)
                VALUES
                    (@organizationId, @responseId, @total, @max, @percentage, @maturityLabel,
                     @radarText, @strategicTruth, @risk, @nextLevel)
                """;
            await transaction.Connection!.ExecuteAsync(sql,
                new { organizationId, responseId, total, max, percentage, maturityLabel, radarText, strategicTruth, risk, nextLevel },
                transaction);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Erro ao salvar resultado. OrganizationId={OrganizationId} ResponseId={ResponseId}", organizationId, responseId);
            throw;
        }
    }

    public async Task SaveDimensionScoresAsync(Guid organizationId, Guid responseId,
        IEnumerable<DimensionScoreInput> dimensions, IDbTransaction transaction)
    {
        try
        {
            const string sql = """
                INSERT INTO valorapesquisa.dimension_scores
                    (organization_id, response_id, dimension_name, score, max_score, percentage, level_label)
                VALUES
                    (@organizationId, @responseId, @DimensionName, @Score, @MaxScore, @Percentage, @LevelLabel)
                """;
            foreach (var dimension in dimensions)
                await transaction.Connection!.ExecuteAsync(sql, new
                {
                    organizationId,
                    responseId,
                    dimension.DimensionName,
                    dimension.Score,
                    dimension.MaxScore,
                    dimension.Percentage,
                    dimension.LevelLabel
                }, transaction);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Erro ao salvar dimensões. OrganizationId={OrganizationId} ResponseId={ResponseId}", organizationId, responseId);
            throw;
        }
    }

    public async Task<IReadOnlyList<DimensionScoreReadModel>> GetDimensionsByResponseIdAsync(Guid responseId)
    {
        try
        {
            using var connection = factory.Create();
            const string sql = """
                SELECT dimension_name AS "DimensionName",
                       score::numeric AS "Score",
                       max_score::numeric AS "MaxScore",
                       percentage::numeric AS "Percentage",
                       level_label AS "LevelLabel"
                  FROM valorapesquisa.dimension_scores
                 WHERE response_id = @responseId
                 ORDER BY dimension_name
                """;
            return (await connection.QueryAsync<DimensionScoreReadModel>(sql, new { responseId })).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Erro ao buscar dimensões do resultado. ResponseId={ResponseId}", responseId);
            throw;
        }
    }
}
