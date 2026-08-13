using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Infrastructure.Repositories;

namespace Valora.Infrastructure.Database;

public sealed class SchemaContractValidator(
    IDbConnectionFactory connectionFactory,
    ILogger<SchemaContractValidator> logger)
{
    private static readonly IReadOnlyDictionary<string, string[]> CriticalContract =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["organizations"] = ["id", "status"],
            ["users"] = ["id", "organization_id", "email", "password_hash", "status", "last_login_at", "deleted_at"],
            ["roles"] = ["id", "code"],
            ["user_roles"] = ["user_id", "role_id"],
            ["user_sessions"] = ["id", "organization_id", "user_id", "expires_at"],
            ["subscriptions"] = ["organization_id", "plan_id", "status"],
            ["plans"] = ["id", "name"],
            ["audit_logs"] = ["id", "organization_id", "user_id", "action", "entity_type", "entity_id", "message", "metadata_json", "correlation_id", "created_at"]
        };

    public async Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT table_name AS "TableName", column_name AS "ColumnName"
              FROM information_schema.columns
             WHERE table_schema = 'valorapesquisa'
               AND table_name = ANY(@Tables)
            """;
        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<(string TableName, string ColumnName)>(
            new CommandDefinition(sql, new { Tables = CriticalContract.Keys.ToArray() }, cancellationToken: cancellationToken));
        var actual = rows.GroupBy(row => row.TableName)
            .ToDictionary(group => group.Key, group => group.Select(row => row.ColumnName).ToHashSet(StringComparer.Ordinal));
        var missing = CriticalContract.SelectMany(table => table.Value
                .Where(column => !actual.TryGetValue(table.Key, out var columns) || !columns.Contains(column))
                .Select(column => $"{table.Key}.{column}"))
            .ToArray();
        if (missing.Length == 0)
        {
            logger.LogInformation("PostgreSQL critical schema contract validated.");
            return;
        }

        throw new InvalidOperationException(
            $"PostgreSQL schema is incompatible. Apply pending migrations. Missing: {string.Join(", ", missing)}");
    }
}
