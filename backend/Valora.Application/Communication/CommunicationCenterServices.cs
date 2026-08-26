using System.Text.RegularExpressions;

namespace Valora.Application.Communication;

public sealed record NotificationRequest(Guid OrganizationId, string Title, string Message, string Type, string Severity,
    IReadOnlyCollection<Guid> UserIds, Guid? CreatedByUserId = null, string? SourceType = null, Guid? SourceId = null, string MetadataJson = "{}");
public sealed record NotificationItem(Guid Id, Guid OrganizationId, string Title, string Message, string Type, string Severity,
    string Status, DateTime? ReadAt, DateTime CreatedAt);
public sealed record OutboxRequest(Guid OrganizationId, Guid? RecipientUserId, string? RecipientEmail, string Subject,
    string? BodyHtml, string? BodyText, string MessageType, DateTime? ScheduledAt = null, string MetadataJson = "{}");
public sealed record OutboxItem(Guid Id, Guid OrganizationId, string Status, int RetryCount, DateTime ScheduledAt);
public sealed record TemplateRequest(Guid? OrganizationId, string Key, string Name, string Subject, string? BodyHtml,
    string? BodyText, IReadOnlyCollection<string> AllowedVariables, Guid? ActorUserId = null);
public sealed record ReminderRuleRequest(Guid OrganizationId, string Name, string Type, int DelayMinutes, string? TemplateKey, Guid? ActorUserId = null);

public interface INotificationRepository
{
    Task<Guid> CreateAsync(NotificationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationItem>> ListForUserAsync(Guid organizationId, Guid userId, string? type, string? status, string? severity, CancellationToken cancellationToken = default);
    Task<bool> MarkReadAsync(Guid organizationId, Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
}
public interface ICommunicationOutboxRepository
{
    Task<Guid> QueueAsync(OutboxRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxItem>> ClaimDueAsync(int batchSize, CancellationToken cancellationToken = default);
    Task RecordAttemptAsync(Guid organizationId, Guid outboxId, bool delivered, string? provider, string? providerMessageId, string? error, CancellationToken cancellationToken = default);
    Task<bool> RequeueAsync(Guid organizationId, Guid outboxId, CancellationToken cancellationToken = default);
}
public interface INotificationTemplateRepository { Task<Guid> SaveAsync(TemplateRequest request, CancellationToken cancellationToken = default); }
public interface IReminderRepository { Task<Guid> CreateRuleAsync(ReminderRuleRequest request, CancellationToken cancellationToken = default); }
public interface ICommunicationAuditRepository { Task WriteAsync(Guid organizationId, string messageType, Guid? messageId, string action, Guid? actorUserId, string metadataJson, CancellationToken cancellationToken = default); }

public sealed class NotificationService(INotificationRepository repository)
{
    public Task<Guid> CreateAsync(NotificationRequest request, CancellationToken ct = default)
    {
        if (request.OrganizationId == Guid.Empty || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Organização, título e mensagem são obrigatórios.");
        if (request.UserIds.Count == 0) throw new ArgumentException("Selecione ao menos um destinatário.");
        return repository.CreateAsync(request, ct);
    }
    public Task<IReadOnlyList<NotificationItem>> ListAsync(Guid organizationId, Guid userId, string? type = null, string? status = null, string? severity = null, CancellationToken ct = default) =>
        repository.ListForUserAsync(organizationId, userId, type, status, severity, ct);
    public Task<bool> MarkAsReadAsync(Guid organizationId, Guid userId, Guid notificationId, CancellationToken ct = default) =>
        repository.MarkReadAsync(organizationId, userId, notificationId, ct);
}

public sealed class CommunicationOutboxService(ICommunicationOutboxRepository repository, ICommunicationAuditRepository audit)
{
    public async Task<Guid> QueueAsync(OutboxRequest request, CancellationToken ct = default)
    {
        if (request.OrganizationId == Guid.Empty || string.IsNullOrWhiteSpace(request.Subject) ||
            (request.RecipientUserId is null && string.IsNullOrWhiteSpace(request.RecipientEmail)))
            throw new ArgumentException("Organização, destinatário e assunto são obrigatórios.");
        var id = await repository.QueueAsync(request, ct);
        await audit.WriteAsync(request.OrganizationId, request.MessageType, id, "queued", null, request.MetadataJson, ct);
        return id;
    }
    public Task<bool> ReprocessAsync(Guid organizationId, Guid id, CancellationToken ct = default) => repository.RequeueAsync(organizationId, id, ct);
}

public sealed partial class EmailTemplateService
{
    public static void ValidatePlaceholders(string content, IReadOnlyCollection<string> allowedVariables)
    {
        var allowed = new HashSet<string>(allowedVariables, StringComparer.OrdinalIgnoreCase);
        var invalid = PlaceholderRegex().Matches(content ?? string.Empty).Select(match => match.Groups[1].Value).Where(value => !allowed.Contains(value)).Distinct().ToArray();
        if (invalid.Length > 0) throw new ArgumentException($"Placeholders não permitidos: {string.Join(", ", invalid)}.");
    }

    [GeneratedRegex(@"\{\{\s*([A-Za-z][A-Za-z0-9_.]*)\s*\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();
}

public sealed class NotificationTemplateService(INotificationTemplateRepository repository)
{
    public Task<Guid> SaveAsync(TemplateRequest request, CancellationToken ct = default)
    {
        EmailTemplateService.ValidatePlaceholders(request.Subject, request.AllowedVariables);
        EmailTemplateService.ValidatePlaceholders(request.BodyHtml ?? string.Empty, request.AllowedVariables);
        EmailTemplateService.ValidatePlaceholders(request.BodyText ?? string.Empty, request.AllowedVariables);
        return repository.SaveAsync(request, ct);
    }
}
public sealed class ReminderService(IReminderRepository repository)
{
    public Task<Guid> CreateRuleAsync(ReminderRuleRequest request, CancellationToken ct = default) =>
        request.DelayMinutes < 0 ? throw new ArgumentException("O prazo do lembrete não pode ser negativo.") : repository.CreateRuleAsync(request, ct);
}
public sealed class CommunicationAuditService(ICommunicationAuditRepository repository)
{
    public Task RegisterAsync(Guid organizationId, string type, Guid? id, string action, Guid? actor, string metadataJson = "{}", CancellationToken ct = default) =>
        repository.WriteAsync(organizationId, type, id, action, actor, metadataJson, ct);
}

public sealed class CreateNotificationUseCase(NotificationService service) { public Task<Guid> ExecuteAsync(NotificationRequest request, CancellationToken ct = default) => service.CreateAsync(request, ct); }
public sealed class MarkNotificationAsReadUseCase(NotificationService service) { public Task<bool> ExecuteAsync(Guid organizationId, Guid userId, Guid id, CancellationToken ct = default) => service.MarkAsReadAsync(organizationId, userId, id, ct); }
public sealed class QueueEmailMessageUseCase(CommunicationOutboxService service) { public Task<Guid> ExecuteAsync(OutboxRequest request, CancellationToken ct = default) => service.QueueAsync(request, ct); }
public sealed class CreateReminderRuleUseCase(ReminderService service) { public Task<Guid> ExecuteAsync(ReminderRuleRequest request, CancellationToken ct = default) => service.CreateRuleAsync(request, ct); }
public sealed class SendDiagnosticInvitationUseCase(CommunicationOutboxService service) { public Task<Guid> ExecuteAsync(OutboxRequest request, CancellationToken ct = default) => service.QueueAsync(request with { MessageType = "diagnostic_invitation" }, ct); }
public sealed class SendReportLinkUseCase(CommunicationOutboxService service) { public Task<Guid> ExecuteAsync(OutboxRequest request, CancellationToken ct = default) => service.QueueAsync(request with { MessageType = "report_link" }, ct); }
public sealed class SendCertificateLinkUseCase(CommunicationOutboxService service) { public Task<Guid> ExecuteAsync(OutboxRequest request, CancellationToken ct = default) => service.QueueAsync(request with { MessageType = "certificate_link" }, ct); }
public sealed class ProcessCommunicationOutboxUseCase(ICommunicationOutboxRepository repository) { public Task<IReadOnlyList<OutboxItem>> ExecuteAsync(int batchSize = 25, CancellationToken ct = default) => repository.ClaimDueAsync(Math.Clamp(batchSize, 1, 100), ct); }
