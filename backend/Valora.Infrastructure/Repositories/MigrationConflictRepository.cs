using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

public sealed class MigrationConflictRepository(IDbConnectionFactory connections) : IMigrationConflictRepository
{
    private const string Projection = "id,batch_id AS BatchId,legacy_collection AS LegacyCollection,legacy_id AS LegacyId,target_entity AS TargetEntity,target_id AS TargetId,conflict_type AS ConflictType,severity,legacy_value_json::text AS LegacyValueMaskedJson,current_value_json::text AS CurrentValueMaskedJson,resolution,resolved_by AS ResolvedBy,resolved_at AS ResolvedAt";

    public async Task AddAsync(MigrationConflictDto conflict, CancellationToken ct = default)
    {
        const string sql = "INSERT INTO valorapesquisa.migration_conflicts(id,batch_id,legacy_collection,legacy_id,target_entity,target_id,conflict_type,severity,legacy_value_json,current_value_json,resolution,resolved_by,resolved_at) VALUES (@Id,@BatchId,@LegacyCollection,@LegacyId,@TargetEntity,@TargetId,@ConflictType,@Severity,CAST(@LegacyValueMaskedJson AS jsonb),CAST(@CurrentValueMaskedJson AS jsonb),@Resolution,@ResolvedBy,@ResolvedAt)";
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, conflict, commandTimeout: 30, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<MigrationConflictDto>> ListByBatchAsync(Guid batchId, CancellationToken ct = default)
    {
        using var connection = connections.Create();
        return (await connection.QueryAsync<MigrationConflictDto>(new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.migration_conflicts WHERE batch_id=@batchId ORDER BY created_at", new { batchId }, commandTimeout: 30, cancellationToken: ct))).AsList();
    }

    public async Task ResolveAsync(Guid conflictId, string resolution, string resolvedBy, CancellationToken ct = default)
    {
        const string sql = "UPDATE valorapesquisa.migration_conflicts SET resolution=@resolution,resolved_by=@resolvedBy,resolved_at=now(),severity='resolved',updated_at=now() WHERE id=@conflictId";
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { conflictId, resolution, resolvedBy }, commandTimeout: 30, cancellationToken: ct));
    }

    public async Task<bool> HasBlockingAsync(Guid batchId, CancellationToken ct = default)
    {
        using var connection = connections.Create();
        return await connection.QuerySingleAsync<bool>(new CommandDefinition("SELECT EXISTS(SELECT 1 FROM valorapesquisa.migration_conflicts WHERE batch_id=@batchId AND severity='blocking' AND resolved_at IS NULL)", new { batchId }, commandTimeout: 30, cancellationToken: ct));
    }
}
