namespace ValoraPesquisa.Domain.Attachments;
public enum AttachmentLinkType { Task, Process, DynamicRecord, Order, Invoice, Delivery, Pod, Document, FormResponse }
public sealed record AttachmentFile(Guid Id, Guid TenantId, string FileName, string ContentType, long SizeBytes, string StorageKey, string Sha256, string Extension, DateTimeOffset CreatedAt);
public sealed record AttachmentLink(Guid Id, Guid AttachmentId, AttachmentLinkType LinkType, Guid EntityId);
public sealed record AttachmentVersion(Guid Id, Guid AttachmentId, int VersionNumber, string StorageKey, string Sha256, DateTimeOffset CreatedAt);
public sealed record AttachmentAudit(Guid Id, Guid AttachmentId, Guid? UserId, string Action, DateTimeOffset CreatedAt);
public sealed record AttachmentThumbnail(Guid Id, Guid AttachmentId, string StorageKey, int Width, int Height);
public sealed record AttachmentSignature(Guid Id, Guid AttachmentId, string SignerName, string SignatureHash, DateTimeOffset SignedAt);
public sealed record AttachmentConfiguration(Guid Id, Guid TenantId, string AllowedExtensionsCsv, long MaxSizeBytes);
