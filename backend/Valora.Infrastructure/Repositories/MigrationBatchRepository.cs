using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

public sealed class MigrationBatchRepository(IDbConnectionFactory connections) : IMigrationBatchRepository
{
    private const int CommandTimeout = 30;
    private const string Projection = """
        id, source_type AS SourceType, source_name AS SourceName, mode, status,
        requested_by AS RequestedBy, started_at AS StartedAt, finished_at AS FinishedAt,
        total_records AS TotalRecords, valid_records AS ValidRecords,
        invalid_records AS InvalidRecords, imported_records AS ImportedRecords,
        skipped_records AS SkippedRecords, conflict_records AS ConflictRecords,
        error_records AS ErrorRecords, summary_json::text AS SummaryMaskedJson
        """;

    public async Task<MigrationBatchDto> CreateAsync(string sourceType, string sourceName, string mode, string requestedBy, CancellationToken ct = default)
    {
        const string sql = $"""
            INSERT INTO valorapesquisa.migration_batches
            (
                source_type,
                source_name,
                mode,
                requested_by
            )
            VALUES
            (
                @SourceType,
                @SourceName,
                @Mode,
                @RequestedBy
            )
            RETURNING {Projection};
            """;
        using var connection = connections.Create();
        return await connection.QuerySingleAsync<MigrationBatchDto>(new CommandDefinition(
            sql,
            new { SourceType = sourceType, SourceName = sourceName, Mode = mode, RequestedBy = requestedBy },
            commandTimeout: CommandTimeout,
            cancellationToken: ct));
    }

    public async Task<IReadOnlyList<MigrationBatchDto>> ListAsync(CancellationToken ct = default)
    {
        using var connection = connections.Create();
        var rows = await connection.QueryAsync<MigrationBatchDto>(new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.migration_batches ORDER BY started_at DESC", commandTimeout: CommandTimeout, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<MigrationBatchDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = connections.Create();
        return await connection.QuerySingleOrDefaultAsync<MigrationBatchDto>(new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.migration_batches WHERE id = @id", new { id }, commandTimeout: CommandTimeout, cancellationToken: ct));
    }

    public async Task UpdateStatusAsync(Guid id, string status, string summaryMaskedJson, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE valorapesquisa.migration_batches
            SET status = @Status,
                summary_json = CAST(@SummaryMaskedJson AS jsonb),
                finished_at = CASE
                    WHEN @Status IN ('completed', 'failed', 'rolled_back') THEN now()
                    ELSE finished_at
                END,
                updated_at = now()
            WHERE id = @Id;
            """;
        using var connection = connections.Create();
        var affectedRows = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, Status = status, SummaryMaskedJson = summaryMaskedJson },
            commandTimeout: CommandTimeout,
            cancellationToken: ct));

        if (affectedRows != 1)
        {
            throw new InvalidOperationException($"Expected to update one migration batch, but updated {affectedRows}.");
        }
    }
}
