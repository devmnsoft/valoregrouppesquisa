using Dapper;
using Valora.Application.Communication;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Repositories;

public sealed class NotificationRepository(IDbConnectionFactory factory) : INotificationRepository
{
    public async Task<Guid> CreateAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        using var connection = factory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        var id = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition("""
            INSERT INTO valorapesquisa.notifications(organization_id,title,message,notification_type,severity,status,source_type,source_id,created_by_user_id,metadata_json)
            VALUES (@OrganizationId,@Title,@Message,@Type,@Severity,'active',@SourceType,@SourceId,@CreatedByUserId,CAST(@MetadataJson AS jsonb)) RETURNING id
            """, request, transaction, cancellationToken: cancellationToken));
        foreach (var userId in request.UserIds.Distinct())
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO valorapesquisa.notification_recipients(notification_id,organization_id,user_id,status)
                VALUES (@id,@organizationId,@userId,'pending')
                """, new { id, request.OrganizationId, userId }, transaction, cancellationToken: cancellationToken));
        transaction.Commit();
        return id;
    }

    public async Task<IReadOnlyList<NotificationItem>> ListForUserAsync(Guid organizationId, Guid userId, string? type, string? status, string? severity, CancellationToken cancellationToken = default)
    {
        using var connection = factory.Create();
        var rows = await connection.QueryAsync<NotificationItem>(new CommandDefinition("""
            SELECT n.id,n.organization_id AS OrganizationId,n.title,n.message,n.notification_type AS Type,n.severity,
                   r.status,r.read_at AS ReadAt,n.created_at AS CreatedAt
              FROM valorapesquisa.notifications n
              JOIN valorapesquisa.notification_recipients r ON r.notification_id=n.id AND r.organization_id=n.organization_id
             WHERE n.organization_id=@organizationId AND r.user_id=@userId AND n.deleted_at IS NULL
               AND (@type IS NULL OR n.notification_type=@type) AND (@status IS NULL OR r.status=@status) AND (@severity IS NULL OR n.severity=@severity)
             ORDER BY n.created_at DESC LIMIT 200
            """, new { organizationId, userId, type, status, severity }, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<bool> MarkReadAsync(Guid organizationId, Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        using var connection = factory.Create();
        return await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE valorapesquisa.notification_recipients SET status='read',read_at=COALESCE(read_at,now()),updated_at=now()
             WHERE organization_id=@organizationId AND user_id=@userId AND notification_id=@notificationId
            """, new { organizationId, userId, notificationId }, cancellationToken: cancellationToken)) == 1;
    }
}

public sealed class CommunicationOutboxRepository(IDbConnectionFactory factory) : ICommunicationOutboxRepository
{
    public async Task<Guid> QueueAsync(OutboxRequest request, CancellationToken cancellationToken = default)
    {
        using var connection = factory.Create();
        return await connection.ExecuteScalarAsync<Guid>(new CommandDefinition("""
            INSERT INTO valorapesquisa.communication_outbox(organization_id,recipient_user_id,recipient_email,subject,body_html,body_text,message_type,status,scheduled_at,metadata_json)
            VALUES (@OrganizationId,@RecipientUserId,@RecipientEmail,@Subject,@BodyHtml,@BodyText,@MessageType,'pending',COALESCE(@ScheduledAt,now()),CAST(@MetadataJson AS jsonb)) RETURNING id
            """, request, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<OutboxItem>> ClaimDueAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        using var connection = factory.Create();
        var rows = await connection.QueryAsync<OutboxItem>(new CommandDefinition("""
            UPDATE valorapesquisa.communication_outbox SET status='processing',updated_at=now()
             WHERE id IN (SELECT id FROM valorapesquisa.communication_outbox WHERE status IN ('pending','awaiting_configuration') AND scheduled_at<=now() AND deleted_at IS NULL ORDER BY scheduled_at FOR UPDATE SKIP LOCKED LIMIT @batchSize)
            RETURNING id,organization_id AS OrganizationId,status,retry_count AS RetryCount,scheduled_at AS ScheduledAt
            """, new { batchSize }, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task RecordAttemptAsync(Guid organizationId, Guid outboxId, bool delivered, string? provider, string? providerMessageId, string? error, CancellationToken cancellationToken = default)
    {
        using var connection = factory.Create(); connection.Open(); using var tx = connection.BeginTransaction();
        var attempt = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT retry_count+1 FROM valorapesquisa.communication_outbox WHERE id=@outboxId AND organization_id=@organizationId FOR UPDATE", new { outboxId, organizationId }, tx, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.communication_delivery_attempts(outbox_id,organization_id,attempt_number,provider,status,provider_message_id,error_message,completed_at) VALUES (@outboxId,@organizationId,@attempt,@provider,@status,@providerMessageId,@error,now())", new { outboxId, organizationId, attempt, provider, status = delivered ? "delivered" : "failed", providerMessageId, error }, tx, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.communication_outbox SET status=@status,sent_at=CASE WHEN @delivered THEN now() ELSE sent_at END,failed_at=CASE WHEN @delivered THEN NULL ELSE now() END,error_message=@error,retry_count=@attempt,updated_at=now() WHERE id=@outboxId AND organization_id=@organizationId", new { outboxId, organizationId, delivered, status = delivered ? "sent" : "failed", error, attempt }, tx, cancellationToken: cancellationToken));
        tx.Commit();
    }

    public async Task<bool> RequeueAsync(Guid organizationId, Guid outboxId, CancellationToken cancellationToken = default)
    {
        using var connection = factory.Create();
        return await connection.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.communication_outbox SET status='pending',scheduled_at=now(),error_message=NULL,updated_at=now() WHERE id=@outboxId AND organization_id=@organizationId AND status IN ('failed','awaiting_configuration') AND deleted_at IS NULL", new { organizationId, outboxId }, cancellationToken: cancellationToken)) == 1;
    }
}

public sealed class CommunicationAuditRepository(IDbConnectionFactory factory) : ICommunicationAuditRepository
{
    public async Task WriteAsync(Guid organizationId, string messageType, Guid? messageId, string action, Guid? actorUserId, string metadataJson, CancellationToken cancellationToken = default)
    {
        using var connection = factory.Create();
        await connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.message_audit_logs(organization_id,message_type,message_id,action,actor_user_id,metadata_json) VALUES (@organizationId,@messageType,@messageId,@action,@actorUserId,CAST(@metadataJson AS jsonb))", new { organizationId, messageType, messageId, action, actorUserId, metadataJson }, cancellationToken: cancellationToken));
    }
}

public sealed class NotificationTemplateRepository(IDbConnectionFactory factory) : INotificationTemplateRepository
{
    public async Task<Guid> SaveAsync(TemplateRequest request, CancellationToken cancellationToken = default)
    {
        using var connection = factory.Create();
        return await connection.ExecuteScalarAsync<Guid>(new CommandDefinition("""
            INSERT INTO valorapesquisa.notification_templates(organization_id,template_key,name,title_template,message_template,allowed_variables,created_by_user_id)
            VALUES (@OrganizationId,@Key,@Name,@Subject,COALESCE(@BodyText,@BodyHtml,''),to_jsonb(CAST(@AllowedVariables AS text[])),@ActorUserId) RETURNING id
            """, request, cancellationToken: cancellationToken));
    }
}

public sealed class ReminderRepository(IDbConnectionFactory factory) : IReminderRepository
{
    public async Task<Guid> CreateRuleAsync(ReminderRuleRequest request, CancellationToken cancellationToken = default)
    {
        using var connection = factory.Create();
        return await connection.ExecuteScalarAsync<Guid>(new CommandDefinition("""
            INSERT INTO valorapesquisa.reminder_rules(organization_id,name,reminder_type,delay_minutes,template_key,created_by_user_id)
            VALUES (@OrganizationId,@Name,@Type,@DelayMinutes,@TemplateKey,@ActorUserId) RETURNING id
            """, request, cancellationToken: cancellationToken));
    }
}
