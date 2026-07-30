using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

public sealed class MigrationSourceFileRepository(IDbConnectionFactory connections) : IMigrationSourceFileRepository
{
    private const string Projection = "id, batch_id AS BatchId, file_name AS FileName, content_type AS ContentType, size_bytes AS SizeBytes, sha256, stored_path AS StoredPath, status, created_at AS CreatedAt";

    public async Task<MigrationSourceFileDto> CreateAsync(Guid? batchId, string fileName, string? contentType, long sizeBytes, string sha256, string? storedPath, string status, CancellationToken ct = default)
    {
        using var connection = connections.Create();
        return await connection.QuerySingleAsync<MigrationSourceFileDto>(new CommandDefinition($"INSERT INTO valorapesquisa.migration_source_files(batch_id,file_name,content_type,size_bytes,sha256,stored_path,status) VALUES (@batchId,@fileName,@contentType,@sizeBytes,@sha256,@storedPath,@status) RETURNING {Projection}", new { batchId, fileName, contentType, sizeBytes, sha256, storedPath, status }, commandTimeout: 30, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<MigrationSourceFileDto>> ListAsync(CancellationToken ct = default)
    {
        using var connection = connections.Create();
        return (await connection.QueryAsync<MigrationSourceFileDto>(new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.migration_source_files ORDER BY created_at DESC", commandTimeout: 30, cancellationToken: ct))).AsList();
    }

    public async Task<MigrationSourceFileDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = connections.Create();
        return await connection.QuerySingleOrDefaultAsync<MigrationSourceFileDto>(new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.migration_source_files WHERE id=@id", new { id }, commandTimeout: 30, cancellationToken: ct));
    }
}
