using Dapper;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Repositories;

public sealed class ApplicationErrorEventRepository(IDbConnectionFactory connections) : IApplicationErrorEventRepository
{
    public async Task AddAsync(Guid? organizationId, string source, string severity, string friendlyMessage,
        string technicalDetail, string correlationId, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO valorapesquisa.application_error_events
                (organization_id,user_id,source,severity,friendly_message,technical_detail,correlation_id)
            VALUES (@organizationId,NULL,@source,@severity,@friendlyMessage,@technicalDetail,@correlationId);
            """;
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql,
            new { organizationId, source, severity, friendlyMessage, technicalDetail, correlationId },
            cancellationToken: cancellationToken));
    }
}
