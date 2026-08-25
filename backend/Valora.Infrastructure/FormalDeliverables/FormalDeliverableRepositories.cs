using System.Text.Json;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.FormalDeliverables;

namespace Valora.Infrastructure.FormalDeliverables;

public sealed class ShareLinkRepository(IDbConnectionFactory connections) : IShareLinkRepository
{
    public async Task SaveAsync(ShareLink link, Guid? createdBy, CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO valorapesquisa.secure_share_links
              (id,organization_id,diagnostic_id,token_hash,public_slug,title,status,expires_at,allow_download,created_by_user_id)
            VALUES (@Id,@OrganizationId,@DiagnosisId,@TokenHash,@DatabaseSlug,'Resultado Valora Insight','active',@ExpiresAt,@AllowDownload,@CreatedBy)
            """, new { link.Id, link.OrganizationId, link.DiagnosisId, link.TokenHash, DatabaseSlug = link.Id.ToString("N"), link.ExpiresAt,
                link.AllowDownload, CreatedBy = createdBy }, cancellationToken: cancellationToken));
    }

    public async Task<ShareLink?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        return await connection.QuerySingleOrDefaultAsync<ShareLink>(new CommandDefinition("""
            SELECT id, organization_id AS OrganizationId, diagnostic_id AS DiagnosisId, token_hash AS TokenHash,
                   public_slug AS PublicSlug, expires_at AS ExpiresAt, allow_download AS AllowDownload,
                   max_access_count AS MaxAccessCount, access_count AS AccessCount, revoked_at AS RevokedAt
            FROM valorapesquisa.secure_share_links
            WHERE token_hash=@TokenHash AND status='active' AND deleted_at IS NULL
            """, new { TokenHash = tokenHash }, cancellationToken: cancellationToken));
    }

    public async Task<bool> RevokeAsync(Guid organizationId, Guid linkId, CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        return await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE valorapesquisa.secure_share_links SET status='revoked',revoked_at=now(),updated_at=now()
            WHERE id=@LinkId AND organization_id=@OrganizationId AND revoked_at IS NULL AND deleted_at IS NULL
            """, new { LinkId = linkId, OrganizationId = organizationId }, cancellationToken: cancellationToken)) == 1;
    }

    public async Task RegisterAccessAsync(Guid linkId, bool downloadRequested, CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE valorapesquisa.secure_share_links SET access_count=access_count+1,updated_at=now()
            WHERE id=@LinkId AND status='active' AND deleted_at IS NULL;
            INSERT INTO valorapesquisa.secure_share_link_access_logs(share_link_id,access_type,was_allowed)
            VALUES (@LinkId,CASE WHEN @DownloadRequested THEN 'download' ELSE 'view' END,true)
            """, new { LinkId=linkId, DownloadRequested=downloadRequested }, transaction, cancellationToken: cancellationToken));
        transaction.Commit();
    }
}

public sealed class DiagnosisDocumentSnapshotProvider(IDbConnectionFactory connections) : IDiagnosisDocumentSnapshotProvider
{
    private sealed record Header(Guid OrganizationId, string OrganizationName, Guid DiagnosisId, string DiagnosisName,
        DateTimeOffset CompletedAt, decimal OverallScore, string MaturityLevel, string MethodologyName, string MethodologyVersion);

    public async Task<DiagnosisDocumentSnapshot?> LoadAsync(Guid organizationId, Guid diagnosisId, CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        var header = await connection.QuerySingleOrDefaultAsync<Header>(new CommandDefinition("""
            SELECT o.id OrganizationId,o.name OrganizationName,r.id DiagnosisId,s.name DiagnosisName,
              rsp.submitted_at CompletedAt,
              CASE WHEN rs.max_score=0 THEN 0 ELSE (rs.total_score::numeric/rs.max_score)*100 END OverallScore,
              CASE WHEN rs.max_score=0 THEN 'Não calculado' WHEN rs.total_score::numeric/rs.max_score >= .8 THEN 'Avançado'
                   WHEN rs.total_score::numeric/rs.max_score >= .6 THEN 'Estruturado' ELSE 'Em desenvolvimento' END MaturityLevel,
              f.name MethodologyName,fv.version::text MethodologyVersion
            FROM valorapesquisa.results r JOIN valorapesquisa.organizations o ON o.id=r.organization_id
            JOIN valorapesquisa.responses rsp ON rsp.id=r.response_id JOIN valorapesquisa.surveys s ON s.id=rsp.survey_id
            JOIN valorapesquisa.form_versions fv ON fv.id=s.form_version_id JOIN valorapesquisa.forms f ON f.id=fv.form_id
            LEFT JOIN valorapesquisa.result_scores rs ON rs.id=r.result_score_id
            WHERE r.id=@DiagnosisId AND r.organization_id=@OrganizationId
            """, new { DiagnosisId = diagnosisId, OrganizationId = organizationId }, cancellationToken: cancellationToken));
        if (header is null) return null;
        var dimensions = (await connection.QueryAsync<DimensionResult>(new CommandDefinition("""
            SELECT d.name Name,CASE WHEN ds.max_score=0 THEN 0 ELSE (ds.score::numeric/ds.max_score)*100 END Score,
              'Resultado consolidado da dimensão' Interpretation
            FROM valorapesquisa.results r JOIN valorapesquisa.dimension_scores ds ON ds.result_score_id=r.result_score_id
            JOIN valorapesquisa.dimensions d ON d.id=ds.dimension_id WHERE r.id=@DiagnosisId ORDER BY d.name
            """, new { DiagnosisId = diagnosisId }, cancellationToken: cancellationToken))).ToArray();
        var recommendations = (await connection.QueryAsync<string>(new CommandDefinition(
            "SELECT text FROM valorapesquisa.result_recommendations WHERE result_id=@DiagnosisId ORDER BY created_at",
            new { DiagnosisId = diagnosisId }, cancellationToken: cancellationToken))).ToArray();
        return new(header.OrganizationId, header.OrganizationName, header.DiagnosisId, header.DiagnosisName,
            header.CompletedAt, header.OverallScore, header.MaturityLevel, header.MethodologyName, header.MethodologyVersion,
            "Resultado consolidado do diagnóstico.", "Leitura baseada nas respostas e scores consolidados.", dimensions,
            [], [], [], [], [], recommendations, [], true);
    }
}

public sealed class DocumentAccessPolicy(IDbConnectionFactory connections) : IDocumentAccessPolicy
{
    public async Task EnsureCanGenerateAsync(Guid organizationId, Guid? userId, DeliverableFormat format, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue) throw new UnauthorizedAccessException("Um usuário autenticado é necessário para gerar documentos.");
        using var connection = connections.Create();
        var allowed = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS(SELECT 1 FROM valorapesquisa.users WHERE id=@UserId AND organization_id=@OrganizationId AND status='active' AND deleted_at IS NULL)",
            new { UserId = userId.Value, OrganizationId = organizationId }, cancellationToken: cancellationToken));
        if (!allowed) throw new UnauthorizedAccessException("Usuário não pertence à organização solicitada.");
    }
}

public sealed class DocumentStore(IDbConnectionFactory connections) : IDocumentStore
{
    public async Task SaveAsync(GeneratedDocument document, Guid? generatedBy, CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO valorapesquisa.formal_documents
              (id,organization_id,diagnosis_id,format,file_name,content_type,content,trace_code,generated_by,generated_at)
            VALUES (@Id,@OrganizationId,@DiagnosisId,@Format,@FileName,@ContentType,@Content,@TraceCode,@GeneratedBy,@GeneratedAt)
            """, new { document.Id, document.OrganizationId, document.DiagnosisId, Format=document.Format.ToString(),
                document.FileName, document.ContentType, document.Content, document.TraceCode, GeneratedBy=generatedBy,
                document.GeneratedAt }, cancellationToken: cancellationToken));
    }
    public async Task<GeneratedDocument?> FindAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        return await connection.QuerySingleOrDefaultAsync<GeneratedDocument>(new CommandDefinition("""
            SELECT id,organization_id OrganizationId,diagnosis_id DiagnosisId,format,file_name FileName,
              content_type ContentType,content,trace_code TraceCode,generated_at GeneratedAt
            FROM valorapesquisa.formal_documents WHERE id=@DocumentId AND organization_id=@OrganizationId
            """, new { DocumentId=documentId, OrganizationId=organizationId }, cancellationToken: cancellationToken));
    }
}

public sealed class ExportAuditService(IDbConnectionFactory connections) : IExportAuditService
{
    public async Task RecordAsync(Guid organizationId, Guid? userId, string action, string resourceType,
        string resourceId, bool succeeded, string? detail = null, CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO valorapesquisa.audit_logs(organization_id,user_id,action,entity_type,entity_id,message,metadata_json)
            VALUES (@OrganizationId,@UserId,@Action,@ResourceType,@ResourceId,@Detail,
                    jsonb_build_object('succeeded',@Succeeded))
            """, new { OrganizationId=organizationId, UserId=userId, Action=action, ResourceType=resourceType,
                ResourceId=resourceId, Detail=detail, Succeeded=succeeded }, cancellationToken: cancellationToken));
    }
}
