using Dapper;
using Valora.Application.Contracts;
using Valora.Infrastructure.Database;

namespace Valora.Infrastructure.Repositories;

public sealed class ResponseRepository(IDbConnectionFactory factory) : IResponseRepository
{
    public async Task<dynamic?> GetResultAsync(Guid responseId)
    {
        using var connection = factory.Create();
        return await connection.QuerySingleOrDefaultAsync(
            """
            SELECT r.id,
                   r.participant_name,
                   rs.total_score AS TotalScore,
                   rs.percentage,
                   rs.maturity_label AS Level,
                   c.certificate_code AS ValidationCode
              FROM valorapesquisa.responses r
              LEFT JOIN valorapesquisa.result_scores rs ON rs.response_id = r.id
              LEFT JOIN valorapesquisa.certificates c ON c.response_id = r.id
             WHERE r.id = @responseId
            """,
            new { responseId });
    }
}
