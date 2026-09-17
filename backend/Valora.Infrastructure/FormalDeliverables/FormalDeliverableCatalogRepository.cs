using System.Text.Json;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.FormalDeliverables;

namespace Valora.Infrastructure.FormalDeliverables;

public sealed class FormalDeliverableCatalogRepository(IDbConnectionFactory connections) : IFormalDeliverableRepository {
    public async Task<(IReadOnlyList<DeliverableListItemDto> Items, int Total)> ListAsync(Guid organizationId, DeliverableListQuery query, CancellationToken cancellationToken = default) {
        using var connection = connections.Create();
        var page = query.ValidPage;
        var pageSize = query.ValidPageSize;
        var offset = (page - 1) * pageSize;
        Guid? diagnosticFilter = Guid.TryParse(query.DiagnosticId, out var diag) ? diag : null;
        const string where = """
            WHERE d.organization_id=@OrganizationId AND d.deleted_at IS NULL
              AND (@Search IS NULL OR d.title ILIKE '%' || @Search || '%' OR s.name ILIKE '%' || @Search || '%')
              AND (@DiagnosticId IS NULL OR d.diagnostic_id=@DiagnosticId)
              AND (@Type IS NULL OR d.deliverable_type=@Type)
              AND (@EditorialStatus IS NULL OR d.editorial_status=@EditorialStatus)
              AND (@ProcessingStatus IS NULL OR d.processing_status=@ProcessingStatus)
              AND (@From IS NULL OR d.created_at >= @From)
              AND (@To IS NULL OR d.created_at <= @To)
            """;
        var args = new {
            OrganizationId = organizationId,
            Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            DiagnosticId = diagnosticFilter,
            Type = string.IsNullOrWhiteSpace(query.Type) ? null : query.Type.Trim(),
            EditorialStatus = string.IsNullOrWhiteSpace(query.EditorialStatus) ? null : query.EditorialStatus.Trim(),
            ProcessingStatus = string.IsNullOrWhiteSpace(query.ProcessingStatus) ? null : query.ProcessingStatus.Trim(),
            From = query.From,
            To = query.To,
            Limit = pageSize,
            Offset = offset
        };
        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition($"""
            SELECT COUNT(*)::int
            FROM valorapesquisa.formal_deliverables d
            LEFT JOIN valorapesquisa.surveys s ON s.id=d.diagnostic_id
            {where}
            """, args, cancellationToken: cancellationToken));
        var rows = await connection.QueryAsync<ListRow>(new CommandDefinition($"""
            SELECT d.id, d.title, d.diagnostic_id AS DiagnosticId, s.name AS DiagnosticName, d.result_id AS ResultId,
                   d.deliverable_type AS Type, d.version_number AS VersionNumber, d.editorial_status AS EditorialStatus,
                   d.processing_status AS ProcessingStatus, d.created_at AS CreatedAt, d.updated_at AS UpdatedAt,
                   u.name AS ResponsibleName, fd.trace_code AS TraceCode,
                   COALESCE(d.document_id, d.file_id) AS DocumentId
            FROM valorapesquisa.formal_deliverables d
            LEFT JOIN valorapesquisa.surveys s ON s.id=d.diagnostic_id
            LEFT JOIN valorapesquisa.users u ON u.id=d.generated_by_user_id
            LEFT JOIN valorapesquisa.formal_documents fd ON fd.id=COALESCE(d.document_id, d.file_id)
            {where}
            ORDER BY d.created_at DESC, d.id DESC
            LIMIT @Limit OFFSET @Offset
            """, args, cancellationToken: cancellationToken));
        var items = rows.Select(MapListItem).ToArray();
        return (items, total);
    }

    public async Task<DeliverableDetailsDto?> GetDetailsAsync(Guid organizationId, Guid deliverableId, CancellationToken cancellationToken = default) {
        using var connection = connections.Create();
        var row = await connection.QuerySingleOrDefaultAsync<DetailRow>(new CommandDefinition("""
            SELECT d.id, d.title, d.diagnostic_id AS DiagnosticId, s.name AS DiagnosticName, d.result_id AS ResultId,
                   d.deliverable_type AS Type, d.version_number AS VersionNumber, d.editorial_status AS EditorialStatus,
                   d.processing_status AS ProcessingStatus, d.created_at AS CreatedAt, d.updated_at AS UpdatedAt,
                   u.name AS ResponsibleName, fd.trace_code AS TraceCode, d.executive_notes AS ExecutiveNotes,
                   d.sections_json AS SectionsJson, d.reviewer_user_id AS ReviewerUserId, rv.name AS ReviewerName,
                   d.published_at AS PublishedAt, pb.name AS PublishedByName, d.source_result_hash AS SourceResultHash,
                   d.methodology_name AS MethodologyName, d.methodology_version AS MethodologyVersion,
                   d.parent_deliverable_id AS ParentDeliverableId, COALESCE(d.document_id, d.file_id) AS DocumentId,
                   fd.content_type AS ContentType, fd.file_name AS FileName, d.metadata_json AS MetadataJson
            FROM valorapesquisa.formal_deliverables d
            LEFT JOIN valorapesquisa.surveys s ON s.id=d.diagnostic_id
            LEFT JOIN valorapesquisa.users u ON u.id=d.generated_by_user_id
            LEFT JOIN valorapesquisa.users rv ON rv.id=d.reviewer_user_id
            LEFT JOIN valorapesquisa.users pb ON pb.id=d.published_by
            LEFT JOIN valorapesquisa.formal_documents fd ON fd.id=COALESCE(d.document_id, d.file_id)
            WHERE d.id=@DeliverableId AND d.organization_id=@OrganizationId AND d.deleted_at IS NULL
            """, new { DeliverableId = deliverableId, OrganizationId = organizationId }, cancellationToken: cancellationToken));
        if (row is null) return null;
        var access = await ListAccessHistoryAsync(organizationId, deliverableId, cancellationToken);
        var shares = await ListShareLinksAsync(organizationId, deliverableId, cancellationToken);
        var sections = ParseStringArray(row.SectionsJson);
        var (limitations, missing) = ParseAlerts(row.MetadataJson);
        var list = MapListItem(new ListRow(row.Id, row.Title, row.DiagnosticId, row.DiagnosticName, row.ResultId, row.Type,
            row.VersionNumber, row.EditorialStatus, row.ProcessingStatus, row.CreatedAt, row.UpdatedAt, row.ResponsibleName, row.TraceCode, row.DocumentId));
        return new DeliverableDetailsDto(
            list.Id, list.Title, list.DiagnosticId, list.DiagnosticName, list.ResultId, list.Type, list.VersionNumber,
            list.EditorialStatus, list.ProcessingStatus, list.CreatedAt, list.UpdatedAt, list.ResponsibleName,
            list.CanOpen, list.CanReview, list.CanDownload, list.CanShare, list.FileAvailable, list.TraceCode,
            row.ExecutiveNotes, sections, row.ReviewerUserId, row.ReviewerName, row.PublishedAt, row.PublishedByName,
            row.SourceResultHash, row.MethodologyName, row.MethodologyVersion, limitations, missing,
            row.ParentDeliverableId, row.DocumentId, row.ContentType, row.FileName, access, shares);
    }

    public async Task<FormalDeliverableEntity?> GetEntityAsync(Guid organizationId, Guid deliverableId, CancellationToken cancellationToken = default) {
        using var connection = connections.Create();
        return await connection.QuerySingleOrDefaultAsync<FormalDeliverableEntity>(new CommandDefinition("""
            SELECT id, organization_id AS OrganizationId, diagnostic_id AS DiagnosticId, result_id AS ResultId,
                   deliverable_type AS DeliverableType, title, status, editorial_status AS EditorialStatus,
                   processing_status AS ProcessingStatus, version_number AS VersionNumber, template_code AS TemplateCode,
                   sections_json AS SectionsJson, executive_notes AS ExecutiveNotes, reviewer_user_id AS ReviewerUserId,
                   source_result_hash AS SourceResultHash, methodology_name AS MethodologyName,
                   methodology_version AS MethodologyVersion, published_at AS PublishedAt, published_by AS PublishedBy,
                   parent_deliverable_id AS ParentDeliverableId, command_id AS CommandId, document_id AS DocumentId,
                   file_id AS FileId, generated_by_user_id AS GeneratedByUserId, metadata_json AS MetadataJson,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM valorapesquisa.formal_deliverables
            WHERE id=@DeliverableId AND organization_id=@OrganizationId AND deleted_at IS NULL
            """, new { DeliverableId = deliverableId, OrganizationId = organizationId }, cancellationToken: cancellationToken));
    }

    public async Task<FormalDeliverableEntity?> FindByCommandIdAsync(Guid organizationId, string commandId, CancellationToken cancellationToken = default) {
        using var connection = connections.Create();
        return await connection.QuerySingleOrDefaultAsync<FormalDeliverableEntity>(new CommandDefinition("""
            SELECT id, organization_id AS OrganizationId, diagnostic_id AS DiagnosticId, result_id AS ResultId,
                   deliverable_type AS DeliverableType, title, status, editorial_status AS EditorialStatus,
                   processing_status AS ProcessingStatus, version_number AS VersionNumber, template_code AS TemplateCode,
                   sections_json AS SectionsJson, executive_notes AS ExecutiveNotes, reviewer_user_id AS ReviewerUserId,
                   source_result_hash AS SourceResultHash, methodology_name AS MethodologyName,
                   methodology_version AS MethodologyVersion, published_at AS PublishedAt, published_by AS PublishedBy,
                   parent_deliverable_id AS ParentDeliverableId, command_id AS CommandId, document_id AS DocumentId,
                   file_id AS FileId, generated_by_user_id AS GeneratedByUserId, metadata_json AS MetadataJson,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM valorapesquisa.formal_deliverables
            WHERE organization_id=@OrganizationId AND command_id=@CommandId AND deleted_at IS NULL
            ORDER BY created_at DESC, id DESC
            LIMIT 1
            """, new { OrganizationId = organizationId, CommandId = commandId }, cancellationToken: cancellationToken));
    }

    public async Task InsertAsync(FormalDeliverableEntity entity, CancellationToken cancellationToken = default) {
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO valorapesquisa.formal_deliverables(
              id,organization_id,diagnostic_id,result_id,deliverable_type,title,description,status,
              generated_by_user_id,file_id,share_link_id,metadata_json,created_at,updated_at,
              editorial_status,processing_status,version_number,template_code,sections_json,executive_notes,
              reviewer_user_id,source_result_hash,methodology_name,methodology_version,published_at,published_by,
              parent_deliverable_id,command_id,document_id)
            VALUES (
              @Id,@OrganizationId,@DiagnosticId,@ResultId,@DeliverableType,@Title,NULL,@Status,
              @GeneratedByUserId,@FileId,NULL,CAST(@MetadataJson AS jsonb),@CreatedAt,@UpdatedAt,
              @EditorialStatus,@ProcessingStatus,@VersionNumber,@TemplateCode,CAST(@SectionsJson AS jsonb),@ExecutiveNotes,
              @ReviewerUserId,@SourceResultHash,@MethodologyName,@MethodologyVersion,@PublishedAt,@PublishedBy,
              @ParentDeliverableId,@CommandId,@DocumentId)
            """, new {
            entity.Id, entity.OrganizationId, entity.DiagnosticId, entity.ResultId, entity.DeliverableType, entity.Title,
            entity.Status, entity.GeneratedByUserId, entity.FileId, entity.MetadataJson, entity.CreatedAt, entity.UpdatedAt,
            entity.EditorialStatus, entity.ProcessingStatus, entity.VersionNumber, entity.TemplateCode, entity.SectionsJson,
            entity.ExecutiveNotes, entity.ReviewerUserId, entity.SourceResultHash, entity.MethodologyName, entity.MethodologyVersion,
            entity.PublishedAt, entity.PublishedBy, entity.ParentDeliverableId, entity.CommandId, entity.DocumentId
        }, cancellationToken: cancellationToken));
    }

    public async Task UpdateLifecycleAsync(FormalDeliverableEntity entity, CancellationToken cancellationToken = default) {
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE valorapesquisa.formal_deliverables SET
              title=@Title, status=@Status, editorial_status=@EditorialStatus, processing_status=@ProcessingStatus,
              version_number=@VersionNumber, template_code=@TemplateCode, sections_json=CAST(@SectionsJson AS jsonb),
              executive_notes=@ExecutiveNotes, reviewer_user_id=@ReviewerUserId, source_result_hash=@SourceResultHash,
              methodology_name=@MethodologyName, methodology_version=@MethodologyVersion, published_at=@PublishedAt,
              published_by=@PublishedBy, parent_deliverable_id=@ParentDeliverableId, command_id=@CommandId,
              document_id=@DocumentId, file_id=@FileId, metadata_json=CAST(@MetadataJson AS jsonb), updated_at=@UpdatedAt
            WHERE id=@Id AND organization_id=@OrganizationId AND deleted_at IS NULL
            """, new {
            entity.Id, entity.OrganizationId, entity.Title, entity.Status, entity.EditorialStatus, entity.ProcessingStatus,
            entity.VersionNumber, entity.TemplateCode, entity.SectionsJson, entity.ExecutiveNotes, entity.ReviewerUserId,
            entity.SourceResultHash, entity.MethodologyName, entity.MethodologyVersion, entity.PublishedAt, entity.PublishedBy,
            entity.ParentDeliverableId, entity.CommandId, entity.DocumentId, entity.FileId, entity.MetadataJson, entity.UpdatedAt
        }, cancellationToken: cancellationToken));
    }

    public async Task MarkSupersededAsync(Guid organizationId, Guid deliverableId, CancellationToken cancellationToken = default) {
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE valorapesquisa.formal_deliverables
            SET editorial_status='superseded', status='superseded', updated_at=now()
            WHERE id=@DeliverableId AND organization_id=@OrganizationId AND deleted_at IS NULL
            """, new { DeliverableId = deliverableId, OrganizationId = organizationId }, cancellationToken: cancellationToken));
    }

    public async Task WriteGenerationJobAsync(Guid organizationId, Guid deliverableId, Guid? userId, string status, string? errorMessage, CancellationToken cancellationToken = default) {
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO valorapesquisa.formal_deliverable_generation_jobs(
              organization_id,deliverable_id,status,requested_by_user_id,error_message,requested_at,started_at,completed_at)
            VALUES (@OrganizationId,@DeliverableId,@Status,@UserId,@ErrorMessage,now(),
                    CASE WHEN @Status IN ('processing','completed','failed') THEN now() ELSE NULL END,
                    CASE WHEN @Status IN ('completed','failed') THEN now() ELSE NULL END)
            """, new { OrganizationId = organizationId, DeliverableId = deliverableId, Status = status, UserId = userId, ErrorMessage = errorMessage },
            cancellationToken: cancellationToken));
    }

    public async Task WriteReportGenerationLogAsync(Guid organizationId, Guid? deliverableId, Guid? resultId, Guid? userId, string format, string status, string? detail, CancellationToken cancellationToken = default) {
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO valorapesquisa.report_generation_logs(
              organization_id,deliverable_id,result_id,format,status,generated_by_user_id,detail)
            VALUES (@OrganizationId,@DeliverableId,@ResultId,@Format,@Status,@UserId,@Detail)
            """, new { OrganizationId = organizationId, DeliverableId = deliverableId, ResultId = resultId, Format = format, Status = status, UserId = userId, Detail = detail },
            cancellationToken: cancellationToken));
    }

    public async Task<EligibleResultInfo?> LoadEligibleResultAsync(Guid organizationId, Guid resultId, CancellationToken cancellationToken = default) {
        using var connection = connections.Create();
        return await connection.QuerySingleOrDefaultAsync<EligibleResultInfo>(new CommandDefinition("""
            SELECT r.id AS ResultId, s.id AS DiagnosticId, s.name AS DiagnosticName, rsp.submitted_at AS SubmittedAt,
                   rs.total_score AS TotalScore, rs.max_score AS MaxScore,
                   CASE WHEN rs.max_score=0 THEN 0 ELSE (rs.total_score::numeric/rs.max_score)*100 END AS Percentage,
                   rs.updated_at AS ScoreUpdatedAt, f.name AS MethodologyName, fv.version::text AS MethodologyVersion
            FROM valorapesquisa.results r
            JOIN valorapesquisa.responses rsp ON rsp.id=r.response_id
            JOIN valorapesquisa.surveys s ON s.id=rsp.survey_id
            JOIN valorapesquisa.form_versions fv ON fv.id=s.form_version_id
            JOIN valorapesquisa.forms f ON f.id=fv.form_id
            JOIN valorapesquisa.result_scores rs ON rs.id=r.result_score_id
            WHERE r.id=@ResultId AND r.organization_id=@OrganizationId
              AND rsp.submitted_at IS NOT NULL AND r.result_score_id IS NOT NULL
            """, new { ResultId = resultId, OrganizationId = organizationId }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ResultOptionDto>> SearchEligibleResultsAsync(Guid organizationId, string? search, int limit, CancellationToken cancellationToken = default) {
        using var connection = connections.Create();
        var rows = await connection.QueryAsync<ResultOptionDto>(new CommandDefinition("""
            SELECT r.id AS Id, s.name AS Name, s.id AS DiagnosticId, s.name AS DiagnosticName,
                   rsp.submitted_at AS CompletedAt,
                   CASE WHEN rs.max_score=0 THEN 0 ELSE (rs.total_score::numeric/rs.max_score)*100 END AS OverallScore
            FROM valorapesquisa.results r
            JOIN valorapesquisa.responses rsp ON rsp.id=r.response_id
            JOIN valorapesquisa.surveys s ON s.id=rsp.survey_id
            JOIN valorapesquisa.result_scores rs ON rs.id=r.result_score_id
            WHERE r.organization_id=@OrganizationId AND rsp.submitted_at IS NOT NULL
              AND (@Search IS NULL OR s.name ILIKE '%' || @Search || '%')
            ORDER BY rsp.submitted_at DESC, r.id DESC
            LIMIT @Limit
            """, new {
            OrganizationId = organizationId,
            Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            Limit = Math.Clamp(limit, 1, 100)
        }, cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    public async Task<IReadOnlyList<TemplateOptionDto>> ListTemplatesAsync(string? type, CancellationToken cancellationToken = default) {
        using var connection = connections.Create();
        var rows = await connection.QueryAsync<(string Code, string Name, string Type, string? ConfigurationJson)>(new CommandDefinition("""
            SELECT COALESCE(template_code, deliverable_type) AS Code, name AS Name, deliverable_type AS Type,
                   COALESCE(configuration_json::text, template_json::text, '{}') AS ConfigurationJson
            FROM valorapesquisa.formal_deliverable_templates
            WHERE is_active=true AND deleted_at IS NULL
              AND (@Type IS NULL OR deliverable_type=@Type)
            ORDER BY name
            """, new { Type = string.IsNullOrWhiteSpace(type) ? null : type.Trim() }, cancellationToken: cancellationToken));
        return rows.Select(r => {
            string? certType = null;
            try {
                using var doc = JsonDocument.Parse(r.ConfigurationJson ?? "{}");
                if (doc.RootElement.TryGetProperty("certificateType", out var el)) certType = el.GetString();
            }
            catch (JsonException) { /* ignore */ }
            return new TemplateOptionDto(r.Code, r.Name, r.Type, certType);
        }).ToArray();
    }

    public async Task<DeliverableTemplateInfo?> GetTemplateAsync(string templateCode, CancellationToken cancellationToken = default) {
        using var connection = connections.Create();
        return await connection.QuerySingleOrDefaultAsync<DeliverableTemplateInfo>(new CommandDefinition("""
            SELECT COALESCE(template_code, deliverable_type) AS Code, name AS Name, deliverable_type AS DeliverableType,
                   COALESCE(configuration_json::text, template_json::text, '{}') AS ConfigurationJson
            FROM valorapesquisa.formal_deliverable_templates
            WHERE is_active=true AND deleted_at IS NULL
              AND (template_code=@Code OR deliverable_type=@Code)
            ORDER BY updated_at DESC
            LIMIT 1
            """, new { Code = templateCode }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ReviewerOptionDto>> ListReviewersAsync(Guid organizationId, string? search, int limit, CancellationToken cancellationToken = default) {
        using var connection = connections.Create();
        var rows = await connection.QueryAsync<ReviewerOptionDto>(new CommandDefinition("""
            SELECT id AS Id, name AS Name, email AS Email
            FROM valorapesquisa.users
            WHERE organization_id=@OrganizationId AND status='active' AND deleted_at IS NULL
              AND (@Search IS NULL OR name ILIKE '%' || @Search || '%' OR email ILIKE '%' || @Search || '%')
            ORDER BY name
            LIMIT @Limit
            """, new {
            OrganizationId = organizationId,
            Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            Limit = Math.Clamp(limit, 1, 100)
        }, cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    public async Task<IReadOnlyList<DeliverableAccessEventDto>> ListAccessHistoryAsync(Guid organizationId, Guid deliverableId, CancellationToken cancellationToken = default) {
        using var connection = connections.Create();
        var rows = await connection.QueryAsync<DeliverableAccessEventDto>(new CommandDefinition("""
            SELECT a.id AS Id, a.share_link_id AS ShareLinkId, a.access_type AS AccessType,
                   a.was_allowed AS WasAllowed, a.accessed_at AS AccessedAt
            FROM valorapesquisa.secure_share_link_access_logs a
            JOIN valorapesquisa.secure_share_links l ON l.id=a.share_link_id
            WHERE l.organization_id=@OrganizationId AND l.deliverable_id=@DeliverableId AND l.deleted_at IS NULL
            ORDER BY a.accessed_at DESC, a.id DESC
            LIMIT 200
            """, new { OrganizationId = organizationId, DeliverableId = deliverableId }, cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    public async Task<IReadOnlyList<DeliverableShareLinkSummaryDto>> ListShareLinksAsync(Guid organizationId, Guid deliverableId, CancellationToken cancellationToken = default) {
        using var connection = connections.Create();
        var rows = await connection.QueryAsync<DeliverableShareLinkSummaryDto>(new CommandDefinition("""
            SELECT id AS Id, expires_at AS ExpiresAt, allow_download AS AllowDownload, status AS Status,
                   access_count AS AccessCount, revoked_at AS RevokedAt, title AS InternalLabel
            FROM valorapesquisa.secure_share_links
            WHERE organization_id=@OrganizationId AND deliverable_id=@DeliverableId AND deleted_at IS NULL
            ORDER BY created_at DESC, id DESC
            """, new { OrganizationId = organizationId, DeliverableId = deliverableId }, cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    private static DeliverableListItemDto MapListItem(ListRow row) {
        var available = string.Equals(row.ProcessingStatus, DeliverableProcessingStatuses.Available, StringComparison.Ordinal) && row.DocumentId.HasValue;
        var published = string.Equals(row.EditorialStatus, DeliverableEditorialStatuses.Published, StringComparison.Ordinal);
        var draft = string.Equals(row.EditorialStatus, DeliverableEditorialStatuses.Draft, StringComparison.Ordinal);
        return new DeliverableListItemDto(
            row.Id, row.Title, row.DiagnosticId, row.DiagnosticName, row.ResultId, row.Type, row.VersionNumber,
            row.EditorialStatus, row.ProcessingStatus, row.CreatedAt, row.UpdatedAt, row.ResponsibleName,
            CanOpen: true, CanReview: draft, CanDownload: available, CanShare: published && available,
            FileAvailable: available, TraceCode: row.TraceCode);
    }

    private static IReadOnlyList<string> ParseStringArray(string? json) {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try {
            return JsonSerializer.Deserialize<string[]>(json) ?? [];
        }
        catch (JsonException) {
            return [];
        }
    }

    private static (IReadOnlyList<string> Limitations, IReadOnlyList<string> Missing) ParseAlerts(string? metadataJson) {
        if (string.IsNullOrWhiteSpace(metadataJson)) return ([], []);
        try {
            using var doc = JsonDocument.Parse(metadataJson);
            var limitations = doc.RootElement.TryGetProperty("limitations", out var lim) && lim.ValueKind == JsonValueKind.Array
                ? lim.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToArray()
                : [];
            var missing = doc.RootElement.TryGetProperty("missingDataAlerts", out var miss) && miss.ValueKind == JsonValueKind.Array
                ? miss.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToArray()
                : [];
            return (limitations, missing);
        }
        catch (JsonException) {
            return ([], []);
        }
    }

    private sealed record ListRow(
        Guid Id, string Title, Guid? DiagnosticId, string? DiagnosticName, Guid? ResultId, string Type,
        int VersionNumber, string EditorialStatus, string ProcessingStatus, DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt, string? ResponsibleName, string? TraceCode, Guid? DocumentId);

    private sealed record DetailRow(
        Guid Id, string Title, Guid? DiagnosticId, string? DiagnosticName, Guid? ResultId, string Type,
        int VersionNumber, string EditorialStatus, string ProcessingStatus, DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt, string? ResponsibleName, string? TraceCode, string? ExecutiveNotes,
        string? SectionsJson, Guid? ReviewerUserId, string? ReviewerName, DateTimeOffset? PublishedAt,
        string? PublishedByName, string? SourceResultHash, string? MethodologyName, string? MethodologyVersion,
        Guid? ParentDeliverableId, Guid? DocumentId, string? ContentType, string? FileName, string? MetadataJson);
}
