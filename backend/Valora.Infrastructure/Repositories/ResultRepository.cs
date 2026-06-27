using System.Data;
using Dapper;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Repositories;

public sealed class ResultRepository(IDbConnectionFactory factory) : IResultRepository
{
    public async Task<dynamic?> GetByResponseAsync(Guid responseId)
    {
        using var connection = factory.Create();
        return await connection.QuerySingleOrDefaultAsync(
            "SELECT * FROM valorapesquisa.result_scores WHERE response_id=@responseId",
            new { responseId });
    }

    public async Task SaveResultAsync(
        Guid organizationId,
        Guid responseId,
        decimal total,
        decimal max,
        decimal percentage,
        string maturityLabel,
        string radarText,
        string strategicTruth,
        string risk,
        string nextLevel,
        IDbTransaction transaction)
    {
        await transaction.Connection!.ExecuteAsync(
            """
            INSERT INTO valorapesquisa.result_scores(
                organization_id,
                response_id,
                total_score,
                max_score,
                percentage,
                maturity_level,
                maturity_label,
                radar_text,
                strategic_truth,
                risk_if_nothing_changes,
                next_level,
                devolutiva_json)
            VALUES (
                @organizationId,
                @responseId,
                @total,
                @max,
                @percentage,
                @maturityLabel,
                @maturityLabel,
                @radarText,
                @strategicTruth,
                @risk,
                @nextLevel,
                jsonb_build_object('source','public-api'))
            """,
            new { organizationId, responseId, total, max, percentage, maturityLabel, radarText, strategicTruth, risk, nextLevel },
            transaction);
    }

    public async Task SaveDimensionScoresAsync(Guid organizationId, Guid responseId, IEnumerable<dynamic> dimensions, IDbTransaction transaction)
    {
        foreach (var dimension in dimensions)
        {
            await transaction.Connection!.ExecuteAsync(
                """
                INSERT INTO valorapesquisa.dimension_scores(
                    organization_id,
                    response_id,
                    dimension_name,
                    score,
                    max_score,
                    percentage,
                    level_label)
                VALUES (
                    @organizationId,
                    @responseId,
                    @DimensionName,
                    @Score,
                    @MaxScore,
                    @Percentage,
                    @LevelLabel)
                """,
                new
                {
                    organizationId,
                    responseId,
                    dimension.DimensionName,
                    dimension.Score,
                    dimension.MaxScore,
                    dimension.Percentage,
                    dimension.LevelLabel
                },
                transaction);
        }
    }

    public async Task<IReadOnlyList<dynamic>> GetDimensionsByResponseIdAsync(Guid responseId)
    {
        using var connection = factory.Create();
        return (await connection.QueryAsync(
            "SELECT * FROM valorapesquisa.dimension_scores WHERE response_id=@responseId ORDER BY dimension_name",
            new { responseId })).ToList();
    }
}
