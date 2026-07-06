namespace ValoraPesquisa.Domain.Notifications;
public enum NotificationChannel { Sistema, EmailFake, WhatsappFake, TelegramFake, MobilePushFake, WebhookFake }
public enum NotificationStatus { Pending, Sent, Read, Failed, BlockedByPreference }
public sealed record Notification(Guid Id, Guid TenantId, string EventCode, string Title, string Body, DateTimeOffset CreatedAt);
public sealed record NotificationRecipient(Guid Id, Guid NotificationId, Guid UserId, NotificationChannel Channel, NotificationStatus Status, DateTimeOffset? ReadAt);
public sealed record NotificationPreference(Guid Id, Guid TenantId, Guid UserId, NotificationChannel Channel, bool Enabled);
public sealed record NotificationTemplate(Guid Id, Guid TenantId, string EventCode, NotificationChannel Channel, string Subject, string BodyTemplate);
public sealed record NotificationPushLog(Guid Id, Guid NotificationRecipientId, string Provider, string Status, string? Error, DateTimeOffset CreatedAt);
