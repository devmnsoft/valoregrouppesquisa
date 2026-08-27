namespace Valora.Application.FormalDeliverables;

public enum DeliverableFormat { Pdf, Docx, Xlsx, Json, CertificatePdf }

public sealed record DimensionResult(string Name, decimal Score, string Interpretation);
public sealed record EvidenceItem(string Dimension, string Description, string Source);
public sealed record ActionItem(string Priority, string Action, string Owner, DateOnly? DueDate);

/// <summary>A privacy-safe, immutable projection used by every formal export.</summary>
public sealed record DiagnosisDocumentSnapshot(
    Guid OrganizationId,
    string OrganizationName,
    Guid DiagnosisId,
    string DiagnosisName,
    DateTimeOffset CompletedAt,
    decimal OverallScore,
    string MaturityLevel,
    string MethodologyName,
    string MethodologyVersion,
    string ExecutiveSummary,
    string StrategicReading,
    IReadOnlyList<DimensionResult> Dimensions,
    IReadOnlyList<EvidenceItem> EvidenceItems,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> Opportunities,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    IReadOnlyList<string> Recommendations,
    IReadOnlyList<ActionItem> ActionPlan,
    IReadOnlyList<string> Limitations,
    bool IsAnonymous = false);

public sealed record GeneratedDocument(
    Guid Id, Guid OrganizationId, Guid DiagnosisId, DeliverableFormat Format,
    string FileName, string ContentType, byte[] Content, string TraceCode,
    DateTimeOffset GeneratedAt);

public sealed record DocumentRequest(Guid OrganizationId, Guid DiagnosisId, DeliverableFormat Format, Guid? UserId);

public interface IDiagnosisDocumentSnapshotProvider
{
    Task<DiagnosisDocumentSnapshot?> LoadAsync(Guid organizationId, Guid diagnosisId, CancellationToken cancellationToken = default);
}

public interface IDocumentAccessPolicy
{
    Task EnsureCanGenerateAsync(Guid organizationId, Guid? userId, DeliverableFormat format, CancellationToken cancellationToken = default);
}

public interface IDocumentStore
{
    Task SaveAsync(GeneratedDocument document, Guid? generatedBy, CancellationToken cancellationToken = default);
    Task<GeneratedDocument?> FindAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default);
}

public interface IExportAuditService
{
    Task RecordAsync(Guid organizationId, Guid? userId, string action, string resourceType,
        string resourceId, bool succeeded, string? detail = null, CancellationToken cancellationToken = default);
}

public interface IExecutiveReportExportService
{
    GeneratedDocument Render(DiagnosisDocumentSnapshot snapshot, DeliverableFormat format, DateTimeOffset generatedAt);
}

public interface IValoraDocumentService
{
    Task<GeneratedDocument> GenerateAsync(DocumentRequest request, CancellationToken cancellationToken = default);
    Task<GeneratedDocument?> OpenForDownloadAsync(Guid organizationId, Guid documentId, Guid? userId, CancellationToken cancellationToken = default);
}

public sealed record ShareLink(Guid Id, Guid OrganizationId, Guid DiagnosisId, string TokenHash,
    string PublicSlug, DateTimeOffset ExpiresAt, bool AllowDownload, int? MaxAccessCount = null,
    int AccessCount = 0, DateTimeOffset? RevokedAt = null);
public sealed record CreatedShareLink(Guid Id, string Token, string PublicSlug, DateTimeOffset ExpiresAt, bool AllowDownload);

public interface IShareLinkRepository
{
    Task SaveAsync(ShareLink link, Guid? createdBy, CancellationToken cancellationToken = default);
    Task<ShareLink?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RegisterAccessAsync(Guid linkId, bool downloadRequested, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(Guid organizationId, Guid linkId, CancellationToken cancellationToken = default);
}

public interface ISecureShareLinkService
{
    Task<CreatedShareLink> CreateAsync(Guid organizationId, Guid diagnosisId, Guid? userId, TimeSpan lifetime, bool allowDownload, CancellationToken cancellationToken = default);
    Task<ShareLink?> ResolveAsync(string token, bool downloadRequested, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(Guid organizationId, Guid linkId, Guid? userId, CancellationToken cancellationToken = default);
}
