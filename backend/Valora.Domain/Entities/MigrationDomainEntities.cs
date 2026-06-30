using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public sealed record OrganizationBranding : AuditableEntity { public Guid OrganizationId { get; init; } public string PrimaryColor { get; init; } = "#0b3d4d"; public string SecondaryColor { get; init; } = "#d7a94b"; public string? LogoUrl { get; init; } public string? PublicSlug { get; init; } }
public sealed record OrganizationSettings : AuditableEntity { public Guid OrganizationId { get; init; } public bool LgpdConsentRequired { get; init; } = true; public bool AllowPublicResults { get; init; } public string TimeZone { get; init; } = "America/Sao_Paulo"; }
public sealed record OrganizationModule : AuditableEntity { public Guid OrganizationId { get; init; } public string ModuleCode { get; init; } = string.Empty; public bool Enabled { get; init; } public string? BlockReason { get; init; } }
public sealed record UserProfile : AuditableEntity { public Guid UserId { get; init; } public Guid? OrganizationId { get; init; } public string RoleCode { get; init; } = string.Empty; public Guid? DepartmentId { get; init; } }
public sealed record Role : AuditableEntity { public string Code { get; init; } = string.Empty; public string Name { get; init; } = string.Empty; public string Scope { get; init; } = string.Empty; }
public sealed record Permission : AuditableEntity { public string Code { get; init; } = string.Empty; public string Description { get; init; } = string.Empty; }
public sealed record RolePermission : AuditableEntity { public Guid RoleId { get; init; } public Guid PermissionId { get; init; } }
public sealed record UserSession : AuditableEntity { public Guid UserId { get; init; } public string RefreshTokenHash { get; init; } = string.Empty; public DateTime ExpiresAt { get; init; } }
public sealed record PasswordResetToken : AuditableEntity { public Guid UserId { get; init; } public string TokenHash { get; init; } = string.Empty; public DateTime ExpiresAt { get; init; } public DateTime? UsedAt { get; init; } }
public sealed record AccessPolicy : AuditableEntity { public string RoleCode { get; init; } = string.Empty; public string ModuleCode { get; init; } = string.Empty; public string PermissionCode { get; init; } = string.Empty; }
public sealed record Department : AuditableEntity { public Guid OrganizationId { get; init; } public string Name { get; init; } = string.Empty; public Guid? UnitId { get; init; } }
public sealed record Employee : AuditableEntity { public Guid OrganizationId { get; init; } public Guid? DepartmentId { get; init; } public string Name { get; init; } = string.Empty; public string Email { get; init; } = string.Empty; }
public sealed record Participant : AuditableEntity { public Guid OrganizationId { get; init; } public string Name { get; init; } = string.Empty; public string Email { get; init; } = string.Empty; public bool LgpdAccepted { get; init; } }
public sealed record FormVersion : AuditableEntity { public Guid FormId { get; init; } public int VersionNumber { get; init; } public string SnapshotJson { get; init; } = "{}"; }
public sealed record SurveyInvite : AuditableEntity { public Guid SurveyId { get; init; } public string Email { get; init; } = string.Empty; public string Status { get; init; } = "pending"; public string TokenHash { get; init; } = string.Empty; }
public sealed record SurveyParticipant : AuditableEntity { public Guid SurveyId { get; init; } public Guid ParticipantId { get; init; } public string Status { get; init; } = "invited"; }
public sealed record ResultRecommendation : AuditableEntity { public Guid ResultScoreId { get; init; } public string Band { get; init; } = string.Empty; public string Recommendation { get; init; } = string.Empty; }
public sealed record CertificateValidation : AuditableEntity { public Guid CertificateId { get; init; } public string ValidationCodeHash { get; init; } = string.Empty; public DateTime? ValidatedAt { get; init; } }
public sealed record EmailTemplate : AuditableEntity { public Guid? OrganizationId { get; init; } public string Code { get; init; } = string.Empty; public string Subject { get; init; } = string.Empty; public string BodyHtml { get; init; } = string.Empty; }
public sealed record Notification : AuditableEntity { public Guid OrganizationId { get; init; } public Guid? UserId { get; init; } public string Title { get; init; } = string.Empty; public string Status { get; init; } = "unread"; }
public sealed record OperationalLog : AuditableEntity { public Guid? OrganizationId { get; init; } public string Level { get; init; } = "Information"; public string Message { get; init; } = string.Empty; public string CorrelationId { get; init; } = string.Empty; }
public sealed record LgpdConsent : AuditableEntity { public Guid OrganizationId { get; init; } public string Email { get; init; } = string.Empty; public string ConsentText { get; init; } = string.Empty; public DateTime AcceptedAt { get; init; } = DateTime.UtcNow; }
public sealed record PrivacyRequest : AuditableEntity { public Guid OrganizationId { get; init; } public string Email { get; init; } = string.Empty; public string RequestType { get; init; } = string.Empty; public string Status { get; init; } = "open"; }
public sealed record DataExportRequest : AuditableEntity { public Guid OrganizationId { get; init; } public string Email { get; init; } = string.Empty; public string Status { get; init; } = "queued"; }
public sealed record SupportTicket : AuditableEntity { public Guid OrganizationId { get; init; } public string Title { get; init; } = string.Empty; public string Status { get; init; } = "open"; }
public sealed record SupportTicketMessage : AuditableEntity { public Guid TicketId { get; init; } public string Message { get; init; } = string.Empty; public bool Internal { get; init; } }
public sealed record SystemEvent : AuditableEntity { public string Code { get; init; } = string.Empty; public string Severity { get; init; } = "info"; public string PayloadJson { get; init; } = "{}"; }
public sealed record MigrationBatch : AuditableEntity { public string Source { get; init; } = string.Empty; public string Status { get; init; } = "created"; public int ImportedCount { get; init; } public int ErrorCount { get; init; } }
public sealed record ImportLog : AuditableEntity { public Guid MigrationBatchId { get; init; } public string EntityType { get; init; } = string.Empty; public string LegacyId { get; init; } = string.Empty; public string Status { get; init; } = string.Empty; public string? Error { get; init; } }
