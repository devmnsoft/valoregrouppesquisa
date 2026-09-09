using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

public sealed class ExportRepository(IDbConnectionFactory connections) : IExportRepository {
    private const string Projection = """
        id AS "Id", organization_id AS "OrganizationId", requested_by AS "RequestedBy",
        COALESCE(entity,'') AS "Entity", format AS "Format", status AS "Status",
        result_file_name AS "ResultFileName", result_mime_type AS "ResultMimeType",
        result_payload AS "ResultPayload", requested_at AS "CreatedAt", completed_at AS "CompletedAt",
        error_message AS "ErrorMessage", checksum_sha256 AS "ChecksumSha256", expires_at AS "ExpiresAt"
        """;

    public async Task<ExportJobDto> CreateAsync(Guid organizationId, Guid? requestedBy, string entity, string format,
        string? filterJson, string correlationId, CancellationToken cancellationToken) {
        const string sql = """
            INSERT INTO valorapesquisa.export_jobs
                (organization_id,requested_by,entity,format,status,filter_json,correlation_id,next_attempt_at)
            VALUES (@organizationId,@requestedBy,@entity,@format,'pending',CAST(COALESCE(@filterJson,'{}') AS jsonb),@correlationId,now())
            RETURNING
            """ + Projection;
        using var connection = connections.Create();
        return await connection.QuerySingleAsync<ExportJobDto>(new CommandDefinition(sql,
            new { organizationId, requestedBy, entity, format, filterJson, correlationId }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ExportWorkItem>> ClaimAsync(string workerId, int take, CancellationToken cancellationToken) {
        const string sql = """
            WITH candidates AS (
                SELECT id FROM valorapesquisa.export_jobs
                WHERE status IN ('pending','failed') AND completed_at IS NULL
                  AND attempts < max_attempts AND next_attempt_at <= now()
                ORDER BY requested_at, id
                FOR UPDATE SKIP LOCKED LIMIT @take
            )
            UPDATE valorapesquisa.export_jobs job
               SET status='processing',locked_at=now(),locked_by=@workerId,attempts=attempts+1,error_message=NULL
              FROM candidates WHERE job.id=candidates.id
            RETURNING job.id AS "Id",job.organization_id AS "OrganizationId",job.entity AS "Entity",
                      job.format AS "Format",job.filter_json::text AS "FilterJson",job.attempts AS "Attempts",
                      job.max_attempts AS "MaxAttempts",COALESCE(job.correlation_id,'') AS "CorrelationId";
            """;
        using var connection = connections.Create();
        return (await connection.QueryAsync<ExportWorkItem>(new CommandDefinition(sql, new { workerId, take }, cancellationToken: cancellationToken))).AsList();
    }

    public async Task CompleteAsync(GeneratedExport export, CancellationToken cancellationToken) {
        const string sql = """
            UPDATE valorapesquisa.export_jobs
               SET status='completed',result_file_name=@FileName,result_mime_type=@MimeType,
                   result_payload=@payload,checksum_sha256=@ChecksumSha256,expires_at=@ExpiresAt,
                   completed_at=now(),locked_at=NULL,locked_by=NULL,error_message=NULL
             WHERE organization_id=@OrganizationId AND id=@JobId AND status='processing';
            """;
        using var connection = connections.Create();
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql,
            new { export.JobId, export.OrganizationId, export.FileName, export.MimeType, payload = Convert.ToBase64String(export.Content), export.ChecksumSha256, export.ExpiresAt }, cancellationToken: cancellationToken));
        if (affected != 1) throw new InvalidOperationException("Export job was not owned by this worker state.");
    }

    public async Task FailAsync(Guid id, string sanitizedError, DateTimeOffset? retryAt, bool deadLetter, CancellationToken cancellationToken) {
        const string sql = """
            UPDATE valorapesquisa.export_jobs
               SET status=CASE WHEN @deadLetter THEN 'dead_letter' ELSE 'failed' END,
                   error_message=@sanitizedError,failed_at=CASE WHEN @deadLetter THEN now() ELSE NULL END,
                   next_attempt_at=COALESCE(@retryAt,next_attempt_at),locked_at=NULL,locked_by=NULL
             WHERE id=@id AND status='processing';
            """;
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { id, sanitizedError, retryAt, deadLetter }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ExportJobDto>> ListAsync(Guid organizationId, CancellationToken cancellationToken) {
        using var connection = connections.Create();
        return (await connection.QueryAsync<ExportJobDto>(new CommandDefinition(
            $"SELECT {Projection} FROM valorapesquisa.export_jobs WHERE organization_id=@organizationId ORDER BY requested_at DESC",
            new { organizationId }, cancellationToken: cancellationToken))).AsList();
    }

    public async Task<ExportJobDto?> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken) {
        using var connection = connections.Create();
        return await connection.QueryFirstOrDefaultAsync<ExportJobDto>(new CommandDefinition(
            $"SELECT {Projection} FROM valorapesquisa.export_jobs WHERE organization_id=@organizationId AND id=@id",
            new { organizationId, id }, cancellationToken: cancellationToken));
    }
}
