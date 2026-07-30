using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

public sealed class MigrationRollbackRepository(IDbConnectionFactory connections) : IMigrationRollbackRepository
{
    public async Task AddAsync(MigrationRollbackItemDto item, CancellationToken ct = default)
    {
        const string sql = "INSERT INTO valorapesquisa.rollback_records(id,batch_id,target_entity,target_id,operation,before_json,after_json,status,rolled_back_at) VALUES (@Id,@BatchId,@TargetEntity,@TargetId,@Operation,CAST(@BeforeMaskedJson AS jsonb),CAST(@AfterMaskedJson AS jsonb),@Status,@RolledBackAt)";
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, item, commandTimeout: 30, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<MigrationRollbackItemDto>> ListByBatchAsync(Guid batchId, CancellationToken ct = default)
    {
        const string sql = "SELECT id,batch_id AS BatchId,target_entity AS TargetEntity,target_id AS TargetId,operation,before_json::text AS BeforeMaskedJson,after_json::text AS AfterMaskedJson,status,rolled_back_at AS RolledBackAt FROM valorapesquisa.rollback_records WHERE batch_id=@batchId ORDER BY created_at";
        using var connection = connections.Create();
        return (await connection.QueryAsync<MigrationRollbackItemDto>(new CommandDefinition(sql, new { batchId }, commandTimeout: 30, cancellationToken: ct))).AsList();
    }

    public async Task MarkRolledBackAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.rollback_records SET status='rolled_back',rolled_back_at=now(),updated_at=now() WHERE id=@id", new { id }, commandTimeout: 30, cancellationToken: ct));
    }
}
