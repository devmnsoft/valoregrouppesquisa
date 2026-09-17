using System.ComponentModel.DataAnnotations;
using Valora.Application.Workspace;

namespace Valora.Application.FormalDeliverables;

public static class DeliverableEditorialStatuses {
    public const string Draft = "draft";
    public const string InReview = "in_review";
    public const string Published = "published";
    public const string Superseded = "superseded";
    public static readonly IReadOnlySet<string> All = new HashSet<string>([Draft, InReview, Published, Superseded], StringComparer.Ordinal);
}

public static class DeliverableProcessingStatuses {
    public const string AwaitingGeneration = "awaiting_generation";
    public const string Processing = "processing";
    public const string Available = "available";
    public const string Failed = "failed";
    public static readonly IReadOnlySet<string> All = new HashSet<string>([AwaitingGeneration, Processing, Available, Failed], StringComparer.Ordinal);
}

public static class DeliverableTypes {
    public const string ExecutiveReport = "executive_report";
    public const string Certificate = "certificate";
    public static readonly IReadOnlySet<string> All = new HashSet<string>([ExecutiveReport, Certificate], StringComparer.Ordinal);
}

public sealed record DeliverableListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? DiagnosticId = null,
    string? Type = null,
    string? EditorialStatus = null,
    string? ProcessingStatus = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Sort = null) {
    public int ValidPage => Math.Max(1, Page);
    public int ValidPageSize => Math.Clamp(PageSize, 1, 50);
}

public sealed record DeliverableListItemDto(
    Guid Id,
    string Title,
    Guid? DiagnosticId,
    string? DiagnosticName,
    Guid? ResultId,
    string Type,
    int VersionNumber,
    string EditorialStatus,
    string ProcessingStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? ResponsibleName,
    bool CanOpen,
    bool CanReview,
    bool CanDownload,
    bool CanShare,
    bool FileAvailable,
    string? TraceCode);

public sealed record DeliverableAccessEventDto(
    Guid Id,
    Guid ShareLinkId,
    string AccessType,
    bool WasAllowed,
    DateTimeOffset AccessedAt);

public sealed record DeliverableShareLinkSummaryDto(
    Guid Id,
    DateTimeOffset ExpiresAt,
    bool AllowDownload,
    string Status,
    int AccessCount,
    DateTimeOffset? RevokedAt,
    string? InternalLabel);

public sealed record DeliverableDetailsDto(
    Guid Id,
    string Title,
    Guid? DiagnosticId,
    string? DiagnosticName,
    Guid? ResultId,
    string Type,
    int VersionNumber,
    string EditorialStatus,
    string ProcessingStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? ResponsibleName,
    bool CanOpen,
    bool CanReview,
    bool CanDownload,
    bool CanShare,
    bool FileAvailable,
    string? TraceCode,
    string? ExecutiveNotes,
    IReadOnlyList<string> Sections,
    Guid? ReviewerUserId,
    string? ReviewerName,
    DateTimeOffset? PublishedAt,
    string? PublishedByName,
    string? SourceResultHash,
    string? MethodologyName,
    string? MethodologyVersion,
    IReadOnlyList<string> Limitations,
    IReadOnlyList<string> MissingDataAlerts,
    Guid? ParentDeliverableId,
    Guid? DocumentId,
    string? ContentType,
    string? FileName,
    IReadOnlyList<DeliverableAccessEventDto> AccessHistory,
    IReadOnlyList<DeliverableShareLinkSummaryDto> ShareLinks);

public sealed class PrepareDeliverableRequest {
    [Required] public Guid DiagnosticId { get; init; }
    [Required] public Guid ResultId { get; init; }
    [StringLength(80)] public string TemplateCode { get; init; } = "executive_valora";
    [Required, StringLength(240, MinimumLength = 3)] public string Title { get; init; } = "";
    public IReadOnlyList<string>? Sections { get; init; }
    [StringLength(4000)] public string? ExecutiveNotes { get; init; }
    public Guid? ReviewerUserId { get; init; }
    [Required, StringLength(80)] public string CommandId { get; init; } = "";
}

public sealed class SubmitReviewRequest {
    [StringLength(2000)] public string? Notes { get; init; }
    [Required, StringLength(80)] public string CommandId { get; init; } = "";
}

public sealed class PublishDeliverableRequest {
    [Range(typeof(bool), "true", "true", ErrorMessage = "Confirme a publicação do entregável.")]
    public bool Confirmed { get; init; }
    public int VersionNumber { get; init; } = 1;
    [Required, StringLength(80)] public string CommandId { get; init; } = "";
    [StringLength(2000)] public string? Notes { get; init; }
}

public sealed class CreateDeliverableShareRequest {
    [Range(1, 2160)] public int ValidForHours { get; init; } = 72;
    public bool AllowDownload { get; init; }
    [StringLength(120)] public string? InternalLabel { get; init; }
    [Range(typeof(bool), "true", "true", ErrorMessage = "Confirme a criação do link seguro.")]
    public bool Confirmed { get; init; }
    [Required, StringLength(80)] public string CommandId { get; init; } = "";
}

public sealed record CertificateEligibilityResult(
    bool Eligible,
    string? CertificateType,
    string? PendingRuleCode,
    string Message);

public sealed record ResultOptionDto(Guid Id, string Name, Guid DiagnosticId, string DiagnosticName, DateTimeOffset CompletedAt, decimal OverallScore);
public sealed record TemplateOptionDto(string Code, string Name, string Type, string? CertificateType);
public sealed record ReviewerOptionDto(Guid Id, string Name, string? Email);

public sealed record FormalDeliverableEntity(
    Guid Id,
    Guid OrganizationId,
    Guid? DiagnosticId,
    Guid? ResultId,
    string DeliverableType,
    string Title,
    string Status,
    string EditorialStatus,
    string ProcessingStatus,
    int VersionNumber,
    string? TemplateCode,
    string SectionsJson,
    string? ExecutiveNotes,
    Guid? ReviewerUserId,
    string? SourceResultHash,
    string? MethodologyName,
    string? MethodologyVersion,
    DateTimeOffset? PublishedAt,
    Guid? PublishedBy,
    Guid? ParentDeliverableId,
    string? CommandId,
    Guid? DocumentId,
    Guid? FileId,
    Guid? GeneratedByUserId,
    string MetadataJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record EligibleResultInfo(
    Guid ResultId,
    Guid DiagnosticId,
    string DiagnosticName,
    DateTimeOffset SubmittedAt,
    decimal TotalScore,
    decimal MaxScore,
    decimal Percentage,
    DateTimeOffset? ScoreUpdatedAt,
    string MethodologyName,
    string MethodologyVersion);

public sealed record DeliverableTemplateInfo(
    string Code,
    string Name,
    string DeliverableType,
    string ConfigurationJson);

public interface IFormalDeliverableRepository {
    Task<(IReadOnlyList<DeliverableListItemDto> Items, int Total)> ListAsync(Guid organizationId, DeliverableListQuery query, CancellationToken cancellationToken = default);
    Task<DeliverableDetailsDto?> GetDetailsAsync(Guid organizationId, Guid deliverableId, CancellationToken cancellationToken = default);
    Task<FormalDeliverableEntity?> GetEntityAsync(Guid organizationId, Guid deliverableId, CancellationToken cancellationToken = default);
    Task<FormalDeliverableEntity?> FindByCommandIdAsync(Guid organizationId, string commandId, CancellationToken cancellationToken = default);
    Task InsertAsync(FormalDeliverableEntity entity, CancellationToken cancellationToken = default);
    Task UpdateLifecycleAsync(FormalDeliverableEntity entity, CancellationToken cancellationToken = default);
    Task MarkSupersededAsync(Guid organizationId, Guid deliverableId, CancellationToken cancellationToken = default);
    Task WriteGenerationJobAsync(Guid organizationId, Guid deliverableId, Guid? userId, string status, string? errorMessage, CancellationToken cancellationToken = default);
    Task WriteReportGenerationLogAsync(Guid organizationId, Guid? deliverableId, Guid? resultId, Guid? userId, string format, string status, string? detail, CancellationToken cancellationToken = default);
    Task<EligibleResultInfo?> LoadEligibleResultAsync(Guid organizationId, Guid resultId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResultOptionDto>> SearchEligibleResultsAsync(Guid organizationId, string? search, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TemplateOptionDto>> ListTemplatesAsync(string? type, CancellationToken cancellationToken = default);
    Task<DeliverableTemplateInfo?> GetTemplateAsync(string templateCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewerOptionDto>> ListReviewersAsync(Guid organizationId, string? search, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeliverableAccessEventDto>> ListAccessHistoryAsync(Guid organizationId, Guid deliverableId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeliverableShareLinkSummaryDto>> ListShareLinksAsync(Guid organizationId, Guid deliverableId, CancellationToken cancellationToken = default);
}

public interface IExecutiveDeliveryService {
    Task<PageResult<DeliverableListItemDto>> ListAsync(Guid organizationId, DeliverableListQuery query, CancellationToken cancellationToken = default);
    Task<DeliverableDetailsDto?> GetAsync(Guid organizationId, Guid deliverableId, CancellationToken cancellationToken = default);
    Task<DeliverableDetailsDto> PrepareAsync(Guid organizationId, Guid userId, PrepareDeliverableRequest request, CancellationToken cancellationToken = default);
    Task<DeliverableDetailsDto> SubmitForReviewAsync(Guid organizationId, Guid userId, Guid deliverableId, SubmitReviewRequest request, CancellationToken cancellationToken = default);
    Task<DeliverableDetailsDto> PublishAsync(Guid organizationId, Guid userId, Guid deliverableId, PublishDeliverableRequest request, CancellationToken cancellationToken = default);
    Task<DeliverableDetailsDto> CreateNewVersionAsync(Guid organizationId, Guid userId, Guid deliverableId, string commandId, CancellationToken cancellationToken = default);
    Task<GeneratedDocument> DownloadAsync(Guid organizationId, Guid userId, Guid deliverableId, CancellationToken cancellationToken = default);
    Task<CreatedShareLink> ShareAsync(Guid organizationId, Guid userId, Guid deliverableId, CreateDeliverableShareRequest request, CancellationToken cancellationToken = default);
    Task<bool> RevokeShareAsync(Guid organizationId, Guid userId, Guid shareLinkId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeliverableAccessEventDto>> ListAccessHistoryAsync(Guid organizationId, Guid deliverableId, CancellationToken cancellationToken = default);
    Task<CertificateEligibilityResult> CertificateEligibilityAsync(Guid organizationId, Guid resultId, string? templateCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResultOptionDto>> EligibleResultsAsync(Guid organizationId, string? search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TemplateOptionDto>> TemplatesAsync(string? type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewerOptionDto>> ReviewersAsync(Guid organizationId, string? search, CancellationToken cancellationToken = default);
}
