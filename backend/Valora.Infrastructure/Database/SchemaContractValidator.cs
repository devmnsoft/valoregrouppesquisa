using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
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
            ["audit_logs"] = ["id", "organization_id", "user_id", "action", "entity_type", "entity_id", "message", "metadata_json", "correlation_id", "created_at", "ip_hash", "user_agent", "severity", "module"],
            ["notifications"] = ["id", "organization_id", "user_id", "title", "message", "read_at", "created_at"],
            ["api_keys"] = ["id", "key_hash"],
            ["saas_modules"] = ["id", "code", "commercial_name", "base_price", "main_route", "access_module_code"],
            ["saas_module_features"] = ["id", "module_id", "code", "permission_code"],
            ["saas_module_prices"] = ["id", "module_id", "currency", "billing_cycle", "amount"],
            ["saas_plans"] = ["id", "code", "monthly_price", "annual_price"],
            ["saas_plan_modules"] = ["plan_id", "module_id", "included", "access_mode"],
            ["client_subscriptions"] = ["id", "client_id", "plan_id", "status"],
            ["client_subscription_modules"] = ["subscription_id", "client_id", "module_id", "status"],
            ["client_module_usage"] = ["client_id", "module_id", "metric_code", "used_quantity"],
            ["client_users"] = ["client_id", "user_id", "status"],
            ["client_profiles"] = ["client_id", "code", "status"],
            ["client_user_profiles"] = ["client_id", "client_user_id", "profile_id"],
            ["client_profile_permissions"] = ["client_id", "profile_id", "permission_code"],
            ["module_access_audit"] = ["client_id", "module_id", "action", "correlation_id"],
            ["subscription_audit_events"] = ["client_id", "event_type", "correlation_id"]
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
