using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

public sealed class MigrationRecordRepository(IDbConnectionFactory connections) : IMigrationRecordRepository
{
    private const string Projection = "id, batch_id AS BatchId, source_file_id AS SourceFileId, legacy_collection AS LegacyCollection, legacy_id AS LegacyId, target_entity AS TargetEntity, target_id AS TargetId, action, status, input_json::text AS InputMaskedJson, normalized_json::text AS NormalizedMaskedJson, error_code AS ErrorCode, error_message AS ErrorMessage";

    public async Task AddAsync(MigrationRecordDto record, CancellationToken ct = default)
    {
        const string sql = "INSERT INTO valorapesquisa.migration_records(id,batch_id,source_file_id,legacy_collection,legacy_id,target_entity,target_id,action,status,input_json,normalized_json,error_code,error_message) VALUES (@Id,@BatchId,@SourceFileId,@LegacyCollection,@LegacyId,@TargetEntity,@TargetId,@Action,@Status,CAST(@InputMaskedJson AS jsonb),CAST(@NormalizedMaskedJson AS jsonb),@ErrorCode,@ErrorMessage)";
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, record, commandTimeout: 30, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<MigrationRecordDto>> ListByBatchAsync(Guid batchId, CancellationToken ct = default)
    {
        using var connection = connections.Create();
        return (await connection.QueryAsync<MigrationRecordDto>(new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.migration_records WHERE batch_id=@batchId ORDER BY created_at", new { batchId }, commandTimeout: 30, cancellationToken: ct))).AsList();
    }
}
