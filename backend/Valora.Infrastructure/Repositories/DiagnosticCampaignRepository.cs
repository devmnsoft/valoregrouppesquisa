using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Valora.Application.CommercialDelivery;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Repositories;

public sealed class DiagnosticCampaignRepository(
    IDbConnectionFactory connections,
    IDbTransactionFactory transactions) : IDiagnosticCampaignRepository {
    public async Task<IReadOnlyList<DiagnosticCampaignDto>> ListAsync(Guid organizationId, CancellationToken ct) {
        using var connection = connections.Create();
        var rows = (await connection.QueryAsync<CampaignRow>(new CommandDefinition(
            CampaignSql + " ORDER BY c.created_at DESC", new { organizationId, surveyId = (Guid?)null },
            cancellationToken: ct))).AsList();
        var result = new List<DiagnosticCampaignDto>(rows.Count);
        foreach (var row in rows) result.Add(await MapAsync(connection, row, ct));
        return result;
    }

    public async Task<DiagnosticCampaignDto?> GetAsync(Guid organizationId, Guid surveyId, CancellationToken ct) {
        using var connection = connections.Create();
        var campaign = await connection.QuerySingleOrDefaultAsync<CampaignRow>(new CommandDefinition(
            CampaignSql + " ORDER BY c.created_at DESC LIMIT 1", new { organizationId, surveyId = (Guid?)surveyId },
            cancellationToken: ct));
        return campaign is null ? null : await MapAsync(connection, campaign, ct);
    }

    public async Task<DiagnosticCampaignDto?> CreateAsync(Guid organizationId, Guid surveyId, Guid userId,
        CreateCampaignRequest request, string correlationId, CancellationToken ct) {
        var recipients = (request.Recipients ?? []).Where(recipient => IsEmail(recipient.Email))
            .DistinctBy(recipient => recipient.Email.Trim(), StringComparer.OrdinalIgnoreCase).ToList();
        await using var unit = await transactions.BeginAsync(ct);
        try {
            var survey = await unit.Connection.QuerySingleOrDefaultAsync<SurveyRow>(new CommandDefinition(
                """
                SELECT id AS "Id", public_url AS "PublicUrl", status AS "Status"
                  FROM valorapesquisa.surveys
                 WHERE id=@surveyId AND organization_id=@organizationId
                   AND COALESCE(is_deleted,false)=false
                """, new { organizationId, surveyId }, unit.Transaction, cancellationToken: ct));
            if (survey is null) return null;
            if (survey.Status is not ("published" or "active"))
                throw new InvalidOperationException("A campanha só pode usar um diagnóstico publicado.");
            var campaignId = Guid.NewGuid();
            await unit.Connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO valorapesquisa.diagnostic_campaigns
                    (id,organization_id,survey_id,name,channel,message_subject,message_body,audience_json,
                     public_url,status,starts_at,ends_at,target_participation_rate,unit_id,department_id,
                     created_by,correlation_id,version)
                VALUES
                    (@campaignId,@organizationId,@surveyId,@name,@channel,@subject,@message,
                     jsonb_build_object('description',@audience),@publicUrl,'draft',@startsAt,@endsAt,
                     @targetParticipationRate,@unitId,@departmentId,@userId,@correlationId,1);
                INSERT INTO valorapesquisa.diagnostic_campaign_messages
                    (organization_id,campaign_id,channel,subject,body,status,correlation_id)
                VALUES(@organizationId,@campaignId,@channel,@subject,@message,'draft',@correlationId);
                """, new {
                    campaignId,
                    organizationId,
                    surveyId,
                    userId,
                    request.Name,
                    request.Channel,
                    request.Subject,
                    request.Message,
                    request.Audience,
                    survey.PublicUrl,
                    request.StartsAt,
                    request.EndsAt,
                    request.TargetParticipationRate,
                    request.UnitId,
                    request.DepartmentId,
                    correlationId
                }, unit.Transaction, cancellationToken: ct));

            foreach (var recipient in recipients) {
                var email = recipient.Email.Trim().ToLowerInvariant();
                var emailJobId = Guid.NewGuid();
                var idempotencyKey = Hash($"{campaignId:N}:{email}:invitation");
                await unit.Connection.ExecuteAsync(new CommandDefinition(
                    """
                    INSERT INTO valorapesquisa.email_jobs
                        (id,organization_id,recipient_email,subject,template_key,payload_json,status,
                         idempotency_key,next_attempt_at,created_at,updated_at)
                    VALUES
                        (@emailJobId,@organizationId,@email,@subject,'diagnostic-campaign',
                         jsonb_build_object('campaignId',@campaignId,'surveyId',@surveyId,'publicUrl',@publicUrl,'message',@body),
                         'queued',@idempotencyKey,'9999-12-31 00:00:00+00'::timestamptz,now(),now())
                    ON CONFLICT(idempotency_key) DO NOTHING;
                    INSERT INTO valorapesquisa.diagnostic_campaign_recipients
                        (organization_id,campaign_id,email_hash,recipient_reference,recipient_hash,recipient_masked,
                         status,correlation_id,email_job_id,idempotency_key,metadata_json)
                    VALUES
                        (@organizationId,@campaignId,@emailHash,@masked,@emailHash,@masked,'pending',@correlationId,
                         @emailJobId,@idempotencyKey,jsonb_build_object('consent',@hasConsent,'legalBasis',@hasLegalBasis))
                    ON CONFLICT(idempotency_key) DO NOTHING;
                    """, new {
                        emailJobId,
                        organizationId,
                        campaignId,
                        surveyId,
                        email,
                        subject = request.Subject ?? request.Name,
                        body = $"{request.Message}\n\n{survey.PublicUrl}",
                        publicUrl = survey.PublicUrl,
                        idempotencyKey,
                        emailHash = Hash(email),
                        masked = Mask(email),
                        recipient.HasConsent,
                        recipient.HasLegalBasis,
                        correlationId
                    }, unit.Transaction, cancellationToken: ct));
            }
            await RecordAsync(unit, organizationId, surveyId, campaignId, userId, "campaign.created",
                correlationId, "Campanha salva como rascunho.", null, ct);
            await unit.CommitAsync();
            return await GetAsync(organizationId, surveyId, ct);
        }
        catch {
            await unit.RollbackAsync();
            throw;
        }
    }

    public async Task<CampaignCommandResult?> TransitionAsync(Guid organizationId, Guid surveyId, Guid userId,
        string targetStatus, CampaignTransitionRequest request, string correlationId, CancellationToken ct) {
        await using var unit = await transactions.BeginAsync(ct);
        try {
            var row = await unit.Connection.QuerySingleOrDefaultAsync<TransitionRow>(new CommandDefinition(
                """
                SELECT c.id AS "Id", c.status AS "Status", c.channel AS "Channel", c.public_url AS "PublicUrl",
                       c.starts_at AS "StartsAt", c.version::bigint AS "Version"
                  FROM valorapesquisa.diagnostic_campaigns c
                 WHERE c.organization_id=@organizationId AND c.survey_id=@surveyId AND c.deleted_at IS NULL
                 ORDER BY c.created_at DESC LIMIT 1
                 FOR UPDATE
                """, new { organizationId, surveyId }, unit.Transaction, cancellationToken: ct));
            if (row is null) return null;
            var effectiveStatus = targetStatus;
            if (targetStatus == DiagnosticCampaignStatus.Sending && row.Channel != "email")
                effectiveStatus = DiagnosticCampaignStatus.Active;
            var affected = await unit.Connection.ExecuteAsync(new CommandDefinition(
                """
                UPDATE valorapesquisa.diagnostic_campaigns
                   SET status=@effectiveStatus, version=version+1, updated_at=now(),
                       scheduled_at=CASE WHEN @effectiveStatus='scheduled' THEN COALESCE(starts_at,now()) ELSE scheduled_at END,
                       sent_at=CASE WHEN @effectiveStatus IN ('sending','active') THEN COALESCE(sent_at,now()) ELSE sent_at END,
                       cancelled_at=CASE WHEN @effectiveStatus='cancelled' THEN now() ELSE cancelled_at END
                 WHERE id=@id AND organization_id=@organizationId AND version=@version;
                """, new { effectiveStatus, row.Id, organizationId, row.Version }, unit.Transaction, cancellationToken: ct));
            if (affected != 1) return null;

            if (effectiveStatus is DiagnosticCampaignStatus.Sending or DiagnosticCampaignStatus.Active) {
                await unit.Connection.ExecuteAsync(new CommandDefinition(
                    """
                    UPDATE valorapesquisa.email_jobs j
                       SET next_attempt_at=now(),status=CASE WHEN j.status IN ('failed','dead_letter') THEN 'retrying' ELSE 'queued' END,updated_at=now()
                      FROM valorapesquisa.diagnostic_campaign_recipients r
                     WHERE r.campaign_id=@id AND r.organization_id=@organizationId AND r.email_job_id=j.id
                       AND r.status IN ('pending','failed') AND j.status NOT IN ('sent','processing');
                    UPDATE valorapesquisa.diagnostic_campaign_recipients
                       SET status='queued',queued_at=now(),updated_at=now()
                     WHERE campaign_id=@id AND organization_id=@organizationId AND status IN ('pending','failed');
                    """, new { row.Id, organizationId }, unit.Transaction, cancellationToken: ct));
            }
            else if (effectiveStatus == DiagnosticCampaignStatus.Scheduled) {
                await unit.Connection.ExecuteAsync(new CommandDefinition(
                    """
                    UPDATE valorapesquisa.email_jobs j SET next_attempt_at=@startsAt,updated_at=now()
                      FROM valorapesquisa.diagnostic_campaign_recipients r
                     WHERE r.campaign_id=@id AND r.email_job_id=j.id AND j.status='queued';
                    """, new { row.Id, startsAt = row.StartsAt ?? DateTimeOffset.UtcNow }, unit.Transaction, cancellationToken: ct));
            }
            else if (effectiveStatus == DiagnosticCampaignStatus.Paused) {
                await unit.Connection.ExecuteAsync(new CommandDefinition(
                    """
                    UPDATE valorapesquisa.email_jobs j SET next_attempt_at='9999-12-31 00:00:00+00'::timestamptz,updated_at=now()
                      FROM valorapesquisa.diagnostic_campaign_recipients r
                     WHERE r.campaign_id=@id AND r.email_job_id=j.id AND j.status IN ('queued','retrying');
                    """, new { row.Id }, unit.Transaction, cancellationToken: ct));
            }
            else if (effectiveStatus == DiagnosticCampaignStatus.Cancelled) {
                await unit.Connection.ExecuteAsync(new CommandDefinition(
                    """
                    UPDATE valorapesquisa.email_jobs j SET status='cancelled',updated_at=now()
                      FROM valorapesquisa.diagnostic_campaign_recipients r
                     WHERE r.campaign_id=@id AND r.email_job_id=j.id AND j.status IN ('queued','retrying','failed','dead_letter');
                    UPDATE valorapesquisa.diagnostic_campaign_recipients
                       SET status='cancelled',cancelled_at=now(),updated_at=now()
                     WHERE campaign_id=@id AND organization_id=@organizationId AND status IN ('pending','queued','failed');
                    """, new { row.Id, organizationId }, unit.Transaction, cancellationToken: ct));
            }

            var message = MessageFor(effectiveStatus);
            await RecordAsync(unit, organizationId, surveyId, row.Id, userId, $"campaign.{effectiveStatus}",
                correlationId, message, request.Justification, ct);
            await unit.CommitAsync();
            return new(row.Id, effectiveStatus, message, row.PublicUrl, row.Version + 1);
        }
        catch {
            await unit.RollbackAsync();
            throw;
        }
    }

    public async Task<CampaignCommandResult?> ResendFailuresAsync(Guid organizationId, Guid surveyId, Guid userId,
        string correlationId, CancellationToken ct) {
        await using var unit = await transactions.BeginAsync(ct);
        var row = await unit.Connection.QuerySingleOrDefaultAsync<TransitionRow>(new CommandDefinition(
            """
            SELECT id AS "Id",status AS "Status",channel AS "Channel",public_url AS "PublicUrl",
                   starts_at AS "StartsAt",version::bigint AS "Version"
              FROM valorapesquisa.diagnostic_campaigns
             WHERE organization_id=@organizationId AND survey_id=@surveyId AND deleted_at IS NULL
             ORDER BY created_at DESC LIMIT 1 FOR UPDATE
            """, new { organizationId, surveyId }, unit.Transaction, cancellationToken: ct));
        if (row is null) return null;
        var retried = await unit.Connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE valorapesquisa.email_jobs j SET status='retrying',next_attempt_at=now(),last_error=NULL,updated_at=now()
              FROM valorapesquisa.diagnostic_campaign_recipients r
             WHERE r.campaign_id=@id AND r.organization_id=@organizationId AND r.email_job_id=j.id
               AND r.status='failed' AND j.status IN ('failed','dead_letter');
            UPDATE valorapesquisa.diagnostic_campaign_recipients
               SET status='queued',error_code=NULL,error_message=NULL,queued_at=now(),updated_at=now()
             WHERE campaign_id=@id AND organization_id=@organizationId AND status='failed';
            """, new { row.Id, organizationId }, unit.Transaction, cancellationToken: ct));
        await RecordAsync(unit, organizationId, surveyId, row.Id, userId, "campaign.failures.requeued",
            correlationId, "Falhas elegíveis foram reenfileiradas sem duplicar entregas confirmadas.", null, ct);
        await unit.CommitAsync();
        return new(row.Id, row.Status, retried > 0 ? "Falhas reenfileiradas." : "Não há falhas elegíveis para reenvio.",
            row.PublicUrl, row.Version);
    }

    public async Task<IReadOnlyList<CampaignHistoryDto>> HistoryAsync(Guid organizationId, Guid surveyId,
        CancellationToken ct) {
        using var connection = connections.Create();
        return (await connection.QueryAsync<CampaignHistoryDto>(new CommandDefinition(
            """
            SELECT e.id AS "Id",e.action AS "Action",e.status AS "Status",
                   NULLIF(e.data->>'userId','')::uuid AS "UserId",
                   NULLIF(e.data->>'justification','') AS "Justification",
                   e.correlation_id AS "CorrelationId",e.created_at AS "CreatedAt"
              FROM valorapesquisa.platform_governance_events e
              JOIN valorapesquisa.diagnostic_campaigns c ON c.id=e.entity_id
             WHERE c.organization_id=@organizationId AND c.survey_id=@surveyId
               AND e.organization_id=@organizationId AND e.entity_type='diagnostic_campaign'
             ORDER BY e.created_at DESC
            """, new { organizationId, surveyId }, cancellationToken: ct))).AsList();
    }

    private static async Task<DiagnosticCampaignDto> MapAsync(System.Data.IDbConnection connection, CampaignRow row,
        CancellationToken ct) {
        var recipients = (await connection.QueryAsync<CampaignRecipientDto>(new CommandDefinition(
            """
            SELECT id AS "Id",recipient_masked AS "MaskedRecipient",status AS "Status",error_code AS "ErrorCode",
                   queued_at AS "QueuedAt",sent_at AS "SentAt",opened_at AS "OpenedAt",responded_at AS "RespondedAt",
                   created_at AS "CreatedAt"
              FROM valorapesquisa.diagnostic_campaign_recipients
             WHERE campaign_id=@id AND deleted_at IS NULL ORDER BY created_at
            """, new { row.Id }, cancellationToken: ct))).AsList();
        return new(row.Id, row.SurveyId, row.Name, row.Status, row.Channel, row.Subject, row.PublicUrl, row.Message,
            row.Audience, row.UnitId, row.DepartmentId, row.StartsAt, row.EndsAt, row.TargetParticipationRate,
            row.RecipientCount, row.QueuedCount, row.SentCount, row.OpenedCount, row.StartedCount,
            row.CompletedCount, row.ExpiredCount, row.ResponseCount, row.FailedCount,
            row.RecipientCount == 0 ? 0 : Math.Round(row.CompletedCount * 100m / row.RecipientCount, 2),
            row.CreatedAt, row.Version, recipients);
    }

    private static Task RecordAsync(IUnitOfWork unit, Guid organizationId, Guid surveyId, Guid campaignId,
        Guid userId, string action, string correlationId, string notification, string? justification, CancellationToken ct) =>
        unit.Connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO valorapesquisa.platform_governance_events
                (organization_id,survey_id,module,entity_type,entity_id,action,code,status,correlation_id,data,metadata_json)
            VALUES(@organizationId,@surveyId,'communications','diagnostic_campaign',@campaignId,@action,@action,
                   'completed',@correlationId,jsonb_build_object('userId',@userId,'justification',@justification),'{}');
            INSERT INTO valorapesquisa.notifications
                (organization_id,user_id,title,message,type,related_module,related_entity_id)
            SELECT @organizationId,id,'Comunicação do diagnóstico',@notification,@action,'communications',@campaignId
              FROM valorapesquisa.users
             WHERE id=@userId AND organization_id=@organizationId AND COALESCE(is_deleted,false)=false;
            """, new { organizationId, surveyId, campaignId, userId, action, correlationId, notification, justification },
            unit.Transaction, cancellationToken: ct));

    private const string CampaignSql = """
        SELECT c.id AS "Id",c.survey_id AS "SurveyId",c.name AS "Name",c.status AS "Status",
               c.channel AS "Channel",c.message_subject AS "Subject",c.public_url AS "PublicUrl",
               COALESCE(c.message_body,m.body) AS "Message",c.audience_json->>'description' AS "Audience",
               c.unit_id AS "UnitId",c.department_id AS "DepartmentId",c.starts_at AS "StartsAt",
               c.ends_at AS "EndsAt",c.target_participation_rate AS "TargetParticipationRate",
               (SELECT COUNT(*)::int FROM valorapesquisa.diagnostic_campaign_recipients r WHERE r.campaign_id=c.id AND r.deleted_at IS NULL) AS "RecipientCount",
               (SELECT COUNT(*)::int FROM valorapesquisa.diagnostic_campaign_recipients r WHERE r.campaign_id=c.id AND r.status='queued' AND r.deleted_at IS NULL) AS "QueuedCount",
               (SELECT COUNT(*)::int FROM valorapesquisa.diagnostic_campaign_recipients r WHERE r.campaign_id=c.id AND r.status='sent' AND r.deleted_at IS NULL) AS "SentCount",
               (SELECT COUNT(*)::int FROM valorapesquisa.diagnostic_campaign_recipients r WHERE r.campaign_id=c.id AND r.opened_at IS NOT NULL AND r.deleted_at IS NULL) AS "OpenedCount",
               (SELECT COUNT(*)::int FROM valorapesquisa.responses x WHERE x.organization_id=c.organization_id AND x.survey_id=c.survey_id AND x.created_at>=c.created_at) AS "StartedCount",
               (SELECT COUNT(*)::int FROM valorapesquisa.responses x WHERE x.organization_id=c.organization_id AND x.survey_id=c.survey_id AND x.created_at>=c.created_at AND x.status IN ('completed','submitted')) AS "CompletedCount",
               (SELECT COUNT(*)::int FROM valorapesquisa.diagnostic_campaign_recipients r WHERE r.campaign_id=c.id AND r.status='expired' AND r.deleted_at IS NULL) AS "ExpiredCount",
               (SELECT COUNT(*)::int FROM valorapesquisa.responses x WHERE x.organization_id=c.organization_id AND x.survey_id=c.survey_id AND x.created_at>=c.created_at) AS "ResponseCount",
               (SELECT COUNT(*)::int FROM valorapesquisa.diagnostic_campaign_recipients r WHERE r.campaign_id=c.id AND r.status='failed' AND r.deleted_at IS NULL) AS "FailedCount",
               c.created_at AS "CreatedAt",c.version::bigint AS "Version"
          FROM valorapesquisa.diagnostic_campaigns c
          LEFT JOIN LATERAL (
              SELECT body FROM valorapesquisa.diagnostic_campaign_messages
               WHERE campaign_id=c.id AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 1
          ) m ON true
         WHERE c.organization_id=@organizationId AND (@surveyId IS NULL OR c.survey_id=@surveyId)
           AND c.deleted_at IS NULL
        """;

    private static string MessageFor(string status) => status switch {
        DiagnosticCampaignStatus.Scheduled => "Campanha agendada.",
        DiagnosticCampaignStatus.Sending => "Convites idempotentes foram encaminhados para a fila.",
        DiagnosticCampaignStatus.Active => "Campanha ativa.",
        DiagnosticCampaignStatus.Paused => "Campanha pausada; entregas ainda não iniciadas aguardam retomada.",
        DiagnosticCampaignStatus.Closed => "Campanha encerrada; novos acessos foram bloqueados.",
        DiagnosticCampaignStatus.Cancelled => "Campanha cancelada; entregas pendentes foram canceladas.",
        DiagnosticCampaignStatus.Failed => "Campanha marcada com falha para investigação.",
        _ => "Estado da campanha atualizado."
    };

    private static bool IsEmail(string value) {
        try { return new MailAddress(value).Address.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase); }
        catch { return false; }
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()))).ToLowerInvariant();

    private static string Mask(string value) {
        var parts = value.Trim().Split('@');
        return parts.Length == 2 && parts[0].Length > 0 ? $"{parts[0][0]}***@{parts[1]}" : "***";
    }

    private sealed record SurveyRow(Guid Id, string? PublicUrl, string Status);
    private sealed record TransitionRow(Guid Id, string Status, string Channel, string? PublicUrl,
        DateTimeOffset? StartsAt, long Version);
    private sealed record CampaignRow(Guid Id, Guid SurveyId, string Name, string Status, string Channel,
        string? Subject, string? PublicUrl, string Message, string? Audience, Guid? UnitId, Guid? DepartmentId,
        DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, decimal? TargetParticipationRate, int RecipientCount,
        int QueuedCount, int SentCount, int OpenedCount, int StartedCount, int CompletedCount, int ExpiredCount,
        int ResponseCount, int FailedCount,
        DateTimeOffset CreatedAt, long Version);
}
