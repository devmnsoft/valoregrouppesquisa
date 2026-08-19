using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Valora.Application.CommercialDelivery;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Repositories;

public sealed class DiagnosticCampaignRepository(IDbConnectionFactory connections, IDbTransactionFactory transactions) : IDiagnosticCampaignRepository
{
    public async Task<DiagnosticCampaignDto?> GetAsync(Guid organizationId, Guid surveyId, CancellationToken ct)
    {
        using var connection = connections.Create();
        var campaign = await connection.QuerySingleOrDefaultAsync<CampaignRow>(new CommandDefinition(CampaignSql +
            " ORDER BY c.created_at DESC LIMIT 1", new { organizationId, surveyId }, cancellationToken: ct));
        return campaign is null ? null : await MapAsync(connection, campaign, ct);
    }

    public async Task<DiagnosticCampaignDto?> CreateAsync(Guid organizationId, Guid surveyId, Guid userId, CreateCampaignRequest request, string correlationId, CancellationToken ct)
    {
        var recipients = (request.Recipients ?? []).Where(x => x.HasConsent && IsEmail(x.Email))
            .DistinctBy(x => x.Email.Trim(), StringComparer.OrdinalIgnoreCase).ToList();
        await using var unit = await transactions.BeginAsync(ct);
        try
        {
            var survey = await unit.Connection.QuerySingleOrDefaultAsync<SurveyRow>(new CommandDefinition(
                "SELECT id,public_url PublicUrl,status FROM valorapesquisa.surveys WHERE id=@surveyId AND organization_id=@organizationId AND coalesce(is_deleted,false)=false",
                new { organizationId, surveyId }, unit.Transaction, cancellationToken: ct));
            if (survey is null) return null;
            if (survey.Status is not ("published" or "active"))
                throw new InvalidOperationException("A campanha só pode ser criada para um diagnóstico publicado e ainda aberto.");
            var id = Guid.NewGuid();
            var publicUrl = survey.PublicUrl;
            await unit.Connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO valorapesquisa.diagnostic_campaigns(id,organization_id,survey_id,name,channel,message_subject,message_body,audience_json,public_url,status,created_by,correlation_id)
                VALUES(@id,@organizationId,@surveyId,@name,@channel,@subject,@message,jsonb_build_object('description',@audience),@publicUrl,@status,@userId,@correlationId);
                INSERT INTO valorapesquisa.diagnostic_campaign_messages(organization_id,campaign_id,channel,subject,body,status,correlation_id)
                VALUES(@organizationId,@id,@channel,@subject,@message,'ready',@correlationId);
                """, new { id, organizationId, surveyId, userId, name = request.Name.Trim(), audience = request.Audience, publicUrl,
                    status = "ready", channel = request.Channel.Trim().ToLowerInvariant(), subject = request.Subject?.Trim(), message = request.Message.Trim(), correlationId }, unit.Transaction, cancellationToken: ct));
            foreach (var recipient in recipients)
            {
                await unit.Connection.ExecuteAsync(new CommandDefinition("""
                    INSERT INTO valorapesquisa.diagnostic_campaign_recipients(organization_id,campaign_id,email_hash,recipient_reference,recipient_hash,recipient_masked,status,correlation_id)
                    VALUES(@organizationId,@id,@hash,@masked,@hash,@masked,'pending',@correlationId)
                    """, new { organizationId, id, hash = Hash(recipient.Email), masked = Mask(recipient.Email), correlationId }, unit.Transaction, cancellationToken: ct));
            }
            await RecordAsync(unit, organizationId, surveyId, id, userId, "campaign.created", correlationId, "Campanha criada", ct);
            await unit.CommitAsync();
            return await GetAsync(organizationId, surveyId, ct);
        }
        catch { await unit.RollbackAsync(); throw; }
    }

    public async Task<CampaignCommandResult?> SendAsync(Guid organizationId, Guid surveyId, Guid userId, bool emailConfigured, string correlationId, CancellationToken ct)
    {
        await using var unit = await transactions.BeginAsync(ct);
        try
        {
            var row = await unit.Connection.QuerySingleOrDefaultAsync<CampaignRow>(new CommandDefinition(CampaignSql + " ORDER BY c.created_at DESC LIMIT 1", new { organizationId, surveyId }, unit.Transaction, cancellationToken: ct));
            if (row is null) return null;
            if (row.Status == "cancelled") return new(row.Id, row.Status, "A campanha foi cancelada e não pode continuar enviando.", row.PublicUrl);
            if (row.PublicUrl is null) return new(row.Id, row.Status, "Publique o diagnóstico antes de distribuir o link público.", null);
            var status = "ready";
            var message = emailConfigured && row.RecipientCount > 0
                ? "A comunicação está configurada, mas nenhum envio foi submetido sem um endereço recuperável autorizado. O link público está disponível para compartilhamento manual."
                : "Envio de e-mail ainda não configurado neste ambiente. O link público está disponível para compartilhamento manual.";
            await unit.Connection.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.diagnostic_campaigns SET status=@status,updated_at=now() WHERE id=@id AND organization_id=@organizationId", new { status, id = row.Id, organizationId }, unit.Transaction, cancellationToken: ct));
            await RecordAsync(unit, organizationId, surveyId, row.Id, userId, emailConfigured ? "campaign.prepared" : "campaign.manual_fallback", correlationId, message, ct);
            await unit.CommitAsync();
            return new(row.Id, status, message, row.PublicUrl);
        }
        catch { await unit.RollbackAsync(); throw; }
    }

    public async Task<CampaignCommandResult?> CancelAsync(Guid organizationId, Guid surveyId, Guid userId, string correlationId, CancellationToken ct)
    {
        await using var unit = await transactions.BeginAsync(ct);
        try
        {
            var row = await unit.Connection.QuerySingleOrDefaultAsync<CampaignRow>(new CommandDefinition(CampaignSql + " ORDER BY c.created_at DESC LIMIT 1", new { organizationId, surveyId }, unit.Transaction, cancellationToken: ct));
            if (row is null) return null;
            await unit.Connection.ExecuteAsync(new CommandDefinition("""
                UPDATE valorapesquisa.diagnostic_campaigns SET status='cancelled',updated_at=now() WHERE id=@id AND organization_id=@organizationId;
                UPDATE valorapesquisa.diagnostic_campaign_recipients SET status='cancelled',cancelled_at=now(),updated_at=now()
                WHERE campaign_id=@id AND organization_id=@organizationId AND status IN ('pending','queued');
                """, new { id = row.Id, organizationId }, unit.Transaction, cancellationToken: ct));
            await RecordAsync(unit, organizationId, surveyId, row.Id, userId, "campaign.cancelled", correlationId, "Campanha cancelada", ct);
            await unit.CommitAsync();
            return new(row.Id, "cancelled", "Campanha cancelada. Nenhum envio pendente continuará.", row.PublicUrl);
        }
        catch { await unit.RollbackAsync(); throw; }
    }

    private static async Task<DiagnosticCampaignDto> MapAsync(System.Data.IDbConnection connection, CampaignRow row, CancellationToken ct)
    {
        var recipients = (await connection.QueryAsync<CampaignRecipientDto>(new CommandDefinition("""
            SELECT id,recipient_reference MaskedRecipient,status,error_code ErrorCode,created_at CreatedAt
            FROM valorapesquisa.diagnostic_campaign_recipients WHERE campaign_id=@id AND deleted_at IS NULL ORDER BY created_at
            """, new { row.Id }, cancellationToken: ct))).ToList();
        return new(row.Id, row.SurveyId, row.Name, row.Status, row.PublicUrl, row.Message, row.RecipientCount, row.SentCount, row.FailedCount, row.CreatedAt, recipients);
    }

    private static async Task RecordAsync(IUnitOfWork unit, Guid organizationId, Guid surveyId, Guid campaignId, Guid userId, string action, string correlationId, string notification, CancellationToken ct) =>
        await unit.Connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO valorapesquisa.platform_governance_events(organization_id,survey_id,module,entity_type,entity_id,action,code,status,correlation_id,data,metadata_json)
            VALUES(@organizationId,@surveyId,'communications','diagnostic_campaign',@campaignId,@action,@action,'completed',@correlationId,jsonb_build_object('userId',@userId),'{}');
            INSERT INTO valorapesquisa.notifications(organization_id,user_id,title,message,type,related_module,related_entity_id)
            SELECT @organizationId,id,'Comunicação do diagnóstico',@notification,@action,'communications',@campaignId
            FROM valorapesquisa.users WHERE id=@userId AND organization_id=@organizationId AND coalesce(is_deleted,false)=false;
            """, new { organizationId, surveyId, campaignId, userId, action, correlationId, notification }, unit.Transaction, cancellationToken: ct));

    private const string CampaignSql = """
        SELECT c.id,c.survey_id SurveyId,c.name,c.status,c.public_url PublicUrl,m.body Message,c.created_at CreatedAt,
          count(r.id)::int RecipientCount,count(r.id) FILTER(WHERE r.status='sent')::int SentCount,
          count(r.id) FILTER(WHERE r.status='failed')::int FailedCount
        FROM valorapesquisa.diagnostic_campaigns c
        JOIN LATERAL (SELECT body FROM valorapesquisa.diagnostic_campaign_messages WHERE campaign_id=c.id AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 1) m ON true
        LEFT JOIN valorapesquisa.diagnostic_campaign_recipients r ON r.campaign_id=c.id AND r.deleted_at IS NULL
        WHERE c.organization_id=@organizationId AND c.survey_id=@surveyId AND c.deleted_at IS NULL
        GROUP BY c.id,m.body
        """;
    private static bool IsEmail(string value) { try { return new MailAddress(value).Address.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase); } catch { return false; } }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()))).ToLowerInvariant();
    private static string Mask(string value) { var parts = value.Trim().Split('@'); return parts.Length == 2 ? $"{parts[0][0]}***@{parts[1]}" : "***"; }
    private sealed record SurveyRow(Guid Id, string? PublicUrl, string Status);
    private sealed record CampaignRow(Guid Id, Guid SurveyId, string Name, string Status, string? PublicUrl, string Message, int RecipientCount, int SentCount, int FailedCount, DateTime CreatedAt);
}
