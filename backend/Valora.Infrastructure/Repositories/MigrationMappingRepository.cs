using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

public sealed class MigrationMappingRepository(IDbConnectionFactory connections) : IMigrationMappingRepository
{
    public async Task AddAsync(MigrationMappingDto mapping, CancellationToken ct = default)
    {
        const string sql = "INSERT INTO valorapesquisa.migration_mappings(id,batch_id,legacy_collection,legacy_id,target_entity,target_id,mapping_key) VALUES (@Id,@BatchId,@LegacyCollection,@LegacyId,@TargetEntity,@TargetId,@MappingKey)";
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, mapping, commandTimeout: 30, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<MigrationMappingDto>> ListByBatchAsync(Guid batchId, CancellationToken ct = default)
    {
        const string sql = "SELECT id,batch_id AS BatchId,legacy_collection AS LegacyCollection,legacy_id AS LegacyId,target_entity AS TargetEntity,target_id AS TargetId,mapping_key AS MappingKey FROM valorapesquisa.migration_mappings WHERE batch_id=@batchId ORDER BY created_at";
        using var connection = connections.Create();
        return (await connection.QueryAsync<MigrationMappingDto>(new CommandDefinition(sql, new { batchId }, commandTimeout: 30, cancellationToken: ct))).AsList();
    }
}
