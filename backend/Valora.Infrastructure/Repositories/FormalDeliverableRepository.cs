using Dapper;
using Valora.Application.Contracts;
using Valora.Application.FormalDeliverables;

namespace Valora.Infrastructure.Repositories;

public sealed class DocumentAccessPolicy(IPermissionRepository permissions) : IDocumentAccessPolicy
{
    public async Task EnsureCanGenerateAsync(Guid organizationId, Guid? userId, DeliverableFormat format,
        CancellationToken cancellationToken = default)
    {
        // Null identifies an internal/system operation. User-initiated operations always require tenant-scoped permission.
        if (userId.HasValue && !await permissions.HasAsync(userId.Value, "reports.generate", organizationId))
            throw new UnauthorizedAccessException($"Usuário sem permissão para gerar {format} nesta organização.");
    }
}

public sealed class DocumentStore(IDbConnectionFactory connections) : IDocumentStore
{
    public async Task SaveAsync(GeneratedDocument document, Guid? generatedBy, CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO valorapesquisa.formal_deliverable_documents
              (id, organization_id, diagnosis_id, format, file_name, content_type, content, trace_code, generated_by, generated_at)
            VALUES (@Id, @OrganizationId, @DiagnosisId, @Format, @FileName, @ContentType, @Content, @TraceCode, @GeneratedBy, @GeneratedAt)
            """, new { document.Id, document.OrganizationId, document.DiagnosisId, Format = document.Format.ToString(),
                document.FileName, document.ContentType, document.Content, document.TraceCode, GeneratedBy = generatedBy,
                document.GeneratedAt }, cancellationToken: cancellationToken));
    }

    public async Task<GeneratedDocument?> FindAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        return await connection.QuerySingleOrDefaultAsync<GeneratedDocument>(new CommandDefinition("""
            SELECT id, organization_id AS OrganizationId, diagnosis_id AS DiagnosisId, format, file_name AS FileName,
                   content_type AS ContentType, content, trace_code AS TraceCode, generated_at AS GeneratedAt
            FROM valorapesquisa.formal_deliverable_documents WHERE id=@DocumentId AND organization_id=@OrganizationId
            """, new { DocumentId = documentId, OrganizationId = organizationId }, cancellationToken: cancellationToken));
    }
}

public sealed class ExportAuditService(IDbConnectionFactory connections) : IExportAuditService
{
    public async Task RecordAsync(Guid organizationId, Guid? userId, string action, string resourceType,
        string resourceId, bool succeeded, string? detail = null, CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO valorapesquisa.audit_logs
              (organization_id, user_id, action, entity_type, entity_id, message, metadata_json, created_at)
            VALUES (@OrganizationId, @UserId, @Action, @ResourceType, @ResourceId, @Detail,
                    jsonb_build_object('succeeded', @Succeeded), now())
            """, new { OrganizationId = organizationId, UserId = userId, Action = action, ResourceType = resourceType,
                ResourceId = resourceId, Detail = detail, Succeeded = succeeded }, cancellationToken: cancellationToken));
    }
}
