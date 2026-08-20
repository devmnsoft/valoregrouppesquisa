using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

public sealed class AuditRepository(
    IDbConnectionFactory connectionFactory,
    ILogger<AuditRepository> logger) : IAuditRepository
{
    private const string InsertSql = """
        INSERT INTO valorapesquisa.audit_logs
            (organization_id, user_id, action, entity_type, entity_id, message,
             metadata_json, correlation_id, created_at, ip_hash, user_agent, severity, module)
        VALUES
            (@OrganizationId, @UserId, @Action, @EntityType, @EntityId, @Message,
             CAST(@MetadataJson AS jsonb), @CorrelationId, COALESCE(@CreatedAt, now()), @IpHash,
             @UserAgent, @Severity, @Module)
        """;

    public Task AddAsync(AuditEntry entry) => LogAsync(entry);

    public async Task LogAsync(AuditEntry entry, IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateMetadata(entry.MetadataJson);

        try
        {
            var command = new CommandDefinition(
                InsertSql,
                entry,
                transaction,
                cancellationToken: CancellationToken.None);

            if (transaction?.Connection is not null)
            {
                await transaction.Connection.ExecuteAsync(command);
                return;
            }

            using var connection = connectionFactory.Create();
            await connection.ExecuteAsync(command);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to append business audit. Action={Action} EntityType={EntityType} EntityId={EntityId} CorrelationId={CorrelationId}",
                entry.Action,
                entry.EntityType,
                entry.EntityId,
                entry.CorrelationId);
            throw;
        }
    }

    public async Task<IReadOnlyList<dynamic>> ListAdminAsync(Guid organizationId, int limit = 100)
    {
        const string sql = """
            SELECT id,
                   organization_id AS OrganizationId,
                   user_id AS UserId,
                   action,
                   entity_type AS EntityType,
                   entity_id AS EntityId,
                   message,
                   metadata_json::text AS MetadataJson,
                   correlation_id AS CorrelationId,
                   ip_hash AS IpHash,
                   user_agent AS UserAgent,
                   severity,
                   module,
                   created_at AS CreatedAt
              FROM valorapesquisa.audit_logs
             WHERE organization_id = @OrganizationId
             ORDER BY created_at DESC
             LIMIT @Limit
            """;

        var safeLimit = Math.Clamp(limit, 1, 500);
        try
        {
            using var connection = connectionFactory.Create();
            var command = new CommandDefinition(
                sql,
                new { OrganizationId = organizationId, Limit = safeLimit },
                cancellationToken: CancellationToken.None);
            return (await connection.QueryAsync(command)).AsList();
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to list append-only audit. OrganizationId={OrganizationId}",
                organizationId);
            throw;
        }
    }

    private static void ValidateMetadata(string metadataJson)
    {
        using var document = JsonDocument.Parse(metadataJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Audit metadata must be a JSON object.", nameof(metadataJson));
        }
    }
}
