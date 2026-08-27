using Dapper;
using Valora.Application.Contracts;
using Valora.Application.Experience;

namespace Valora.Infrastructure.Repositories;

public sealed class RespondentAccessTokenRepository(Application.Contracts.IDbConnectionFactory connections) : IRespondentAccessTokenRepository {
    public async Task<Guid> CreateAsync(Guid organizationId, Guid diagnosticId, Guid respondentId, string tokenHash, DateTimeOffset expiresAt, CancellationToken ct) {
        var id = Guid.NewGuid(); using var db = connections.Create();
        await db.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.respondent_access_tokens(id,organization_id,diagnostic_id,respondent_id,token_hash,expires_at) VALUES(@id,@organizationId,@diagnosticId,@respondentId,@tokenHash,@expiresAt)", new { id, organizationId, diagnosticId, respondentId, tokenHash, expiresAt }, cancellationToken: ct));
        return id;
    }
    public async Task<RespondentAccessToken?> ResolveAsync(string tokenHash, CancellationToken ct) { using var db = connections.Create(); return await db.QuerySingleOrDefaultAsync<RespondentAccessToken>(new CommandDefinition("SELECT id,organization_id,diagnostic_id,respondent_id,status,expires_at,first_access_at,completed_at FROM valorapesquisa.respondent_access_tokens WHERE token_hash=@tokenHash AND deleted_at IS NULL", new { tokenHash }, cancellationToken: ct)); }
    public async Task MarkOpenedAsync(Guid id, CancellationToken ct) { using var db = connections.Create(); await db.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.respondent_access_tokens SET first_access_at=COALESCE(first_access_at,now()),last_access_at=now(),status=CASE WHEN status='pending' THEN 'opened' ELSE status END,updated_at=now() WHERE id=@id", new { id }, cancellationToken: ct)); }
    public async Task MarkCompletedAsync(Guid id, CancellationToken ct) { using var db = connections.Create(); await db.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.respondent_access_tokens SET status='completed',completed_at=now(),updated_at=now() WHERE id=@id", new { id }, cancellationToken: ct)); }
}

public sealed class RespondentSessionRepository(IDbConnectionFactory connections) : IRespondentSessionRepository {
    public async Task<RespondentSession> StartOrResumeAsync(RespondentAccessToken token, string? ipAddress, string? userAgent, CancellationToken ct) {
        using var db = connections.Create();
        const string sql = """
            INSERT INTO valorapesquisa.respondent_sessions(id,organization_id,diagnostic_id,respondent_id,access_token_id,ip_address,user_agent)
            VALUES(gen_random_uuid(),@OrganizationId,@DiagnosticId,@RespondentId,@Id,CAST(@ipAddress AS inet),@userAgent)
            ON CONFLICT(access_token_id) WHERE deleted_at IS NULL DO UPDATE SET last_access_at=now(),updated_at=now()
            RETURNING id,organization_id,diagnostic_id,respondent_id,access_token_id,status,current_section_id,progress_percent,started_at,completed_at
            """;
        return await db.QuerySingleAsync<RespondentSession>(new CommandDefinition(sql, new { token.OrganizationId, token.DiagnosticId, token.RespondentId, token.Id, ipAddress, userAgent }, cancellationToken: ct));
    }
    public async Task SaveProgressAsync(RespondentSession session, SaveRespondentProgressRequest request, CancellationToken ct) { using var db = connections.Create(); const string sql = "UPDATE valorapesquisa.respondent_sessions SET current_section_id=@CurrentSectionId,progress_percent=@ProgressPercent,updated_at=now() WHERE id=@Id AND organization_id=@OrganizationId AND status='in_progress'; INSERT INTO valorapesquisa.respondent_progress(session_id,organization_id,section_id,progress_percent,answers_json) VALUES(@Id,@OrganizationId,@CurrentSectionId,@ProgressPercent,CAST(@AnswersJson AS jsonb)) ON CONFLICT(session_id,section_id) DO UPDATE SET progress_percent=excluded.progress_percent,answers_json=excluded.answers_json,updated_at=now(); INSERT INTO valorapesquisa.respondent_session_events(session_id,organization_id,event_type) VALUES(@Id,@OrganizationId,'respondent.answer.saved')"; await db.ExecuteAsync(new CommandDefinition(sql, new { session.Id, session.OrganizationId, request.CurrentSectionId, request.ProgressPercent, request.AnswersJson }, cancellationToken: ct)); }
    public async Task CompleteAsync(RespondentSession session, CancellationToken ct) { using var db = connections.Create(); const string sql = "UPDATE valorapesquisa.respondent_sessions SET status='completed',progress_percent=100,completed_at=now(),updated_at=now() WHERE id=@Id AND organization_id=@OrganizationId AND status='in_progress'; UPDATE valorapesquisa.respondent_access_tokens SET status='completed',completed_at=now(),updated_at=now() WHERE id=@AccessTokenId AND organization_id=@OrganizationId; INSERT INTO valorapesquisa.respondent_session_events(session_id,organization_id,event_type) VALUES(@Id,@OrganizationId,'respondent.session.completed')"; await db.ExecuteAsync(new CommandDefinition(sql, session, cancellationToken: ct)); }
}

public sealed class PublicResultViewRepository(IDbConnectionFactory connections) : IPublicResultViewRepository {
    public async Task<Guid> CreateAsync(Guid organizationId, Guid diagnosticId, Guid resultId, string title, string tokenHash, DateTimeOffset expiresAt, bool allowReport, bool allowCertificate, CancellationToken ct) { var id = Guid.NewGuid(); using var db = connections.Create(); await db.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.public_result_views(id,organization_id,diagnostic_id,result_id,title,share_token_hash,expires_at,metadata_json) VALUES(@id,@organizationId,@diagnosticId,@resultId,@title,@tokenHash,@expiresAt,jsonb_build_object('allow_report',@allowReport,'allow_certificate',@allowCertificate))", new { id, organizationId, diagnosticId, resultId, title, tokenHash, expiresAt, allowReport, allowCertificate }, cancellationToken: ct)); return id; }
    public async Task<PublicResultView?> ResolveAsync(string tokenHash, CancellationToken ct) { using var db = connections.Create(); return await db.QuerySingleOrDefaultAsync<PublicResultView>(new CommandDefinition("SELECT id,organization_id,diagnostic_id,result_id,title,status,expires_at,access_count,COALESCE((metadata_json->>'allow_report')::boolean,false) allow_report,COALESCE((metadata_json->>'allow_certificate')::boolean,false) allow_certificate FROM valorapesquisa.public_result_views WHERE share_token_hash=@tokenHash AND deleted_at IS NULL", new { tokenHash }, cancellationToken: ct)); }
    public async Task RegisterAccessAsync(PublicResultView view, string eventType, string? ipAddress, string? userAgent, string correlationId, CancellationToken ct) { using var db = connections.Create(); const string sql = "UPDATE valorapesquisa.public_result_views SET first_access_at=COALESCE(first_access_at,now()),last_access_at=now(),access_count=access_count+1,updated_at=now() WHERE id=@Id AND organization_id=@OrganizationId; INSERT INTO valorapesquisa.public_result_view_events(public_result_view_id,organization_id,event_type,ip_address,user_agent,correlation_id) VALUES(@Id,@OrganizationId,@eventType,CAST(@ipAddress AS inet),@userAgent,@correlationId)"; await db.ExecuteAsync(new CommandDefinition(sql, new { view.Id, view.OrganizationId, eventType, ipAddress, userAgent, correlationId }, cancellationToken: ct)); }
    public async Task RegisterCertificateDownloadAsync(PublicResultView view, string correlationId, CancellationToken ct) { using var db = connections.Create(); await db.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.public_certificate_downloads(public_result_view_id,organization_id,result_id,correlation_id) VALUES(@Id,@OrganizationId,@ResultId,@correlationId)", new { view.Id, view.OrganizationId, view.ResultId, correlationId }, cancellationToken: ct)); }
}

public sealed class DiagnosticInvitationBatchRepository : IDiagnosticInvitationBatchRepository;
public sealed class ExecutiveResultPortalRepository : IExecutiveResultPortalRepository;
