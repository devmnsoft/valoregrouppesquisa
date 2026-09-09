namespace Valora.Application.CommercialDelivery;

public static class DiagnosticCampaignStatus {
    public const string Draft = "draft";
    public const string Scheduled = "scheduled";
    public const string Sending = "sending";
    public const string Active = "active";
    public const string Paused = "paused";
    public const string Closed = "closed";
    public const string Cancelled = "cancelled";
    public const string Failed = "failed";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(
        [Draft, Scheduled, Sending, Active, Paused, Closed, Cancelled, Failed],
        StringComparer.Ordinal);
}

public sealed record CampaignRecipientRequest(string Email, bool HasConsent = false, bool HasLegalBasis = false);
public sealed record CreateCampaignRequest(
    string Name,
    string Message,
    IReadOnlyList<CampaignRecipientRequest>? Recipients,
    string? Audience = null,
    string Channel = "manual",
    string? Subject = null,
    Guid? UnitId = null,
    Guid? DepartmentId = null,
    DateTimeOffset? StartsAt = null,
    DateTimeOffset? EndsAt = null,
    decimal? TargetParticipationRate = null);
public sealed record CampaignRecipientDto(Guid Id, string MaskedRecipient, string Status, string? ErrorCode,
    DateTimeOffset? QueuedAt, DateTimeOffset? SentAt, DateTimeOffset? OpenedAt, DateTimeOffset? RespondedAt,
    DateTimeOffset CreatedAt);
public sealed record CampaignMetricsDto(int Recipients, int Queued, int Sent, int Opened, int Started,
    int Completed, int Expired, int Failed, decimal CompletionRate, DateTimeOffset UpdatedAt);
public sealed record CampaignHistoryDto(Guid Id, string Action, string Status, Guid? UserId,
    string? Justification, string? CorrelationId, DateTimeOffset CreatedAt);
public sealed record CampaignTransitionRequest(string? Justification = null, long? ExpectedVersion = null);
public sealed record DiagnosticCampaignDto(
    Guid Id,
    Guid SurveyId,
    string Name,
    string Status,
    string Channel,
    string? Subject,
    string? PublicUrl,
    string Message,
    string? Audience,
    Guid? UnitId,
    Guid? DepartmentId,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    decimal? TargetParticipationRate,
    int RecipientCount,
    int QueuedCount,
    int SentCount,
    int OpenedCount,
    int StartedCount,
    int CompletedCount,
    int ExpiredCount,
    int ResponseCount,
    int FailedCount,
    decimal CompletionRate,
    DateTimeOffset CreatedAt,
    long Version,
    IReadOnlyList<CampaignRecipientDto> Recipients);
public sealed record CampaignCommandResult(Guid CampaignId, string Status, string Message, string? PublicUrl, long Version);

public interface IDiagnosticCampaignRepository {
    Task<IReadOnlyList<DiagnosticCampaignDto>> ListAsync(Guid organizationId, CancellationToken ct);
    Task<DiagnosticCampaignDto?> GetAsync(Guid organizationId, Guid surveyId, CancellationToken ct);
    Task<DiagnosticCampaignDto?> CreateAsync(Guid organizationId, Guid surveyId, Guid userId, CreateCampaignRequest request, string correlationId, CancellationToken ct);
    Task<CampaignCommandResult?> TransitionAsync(Guid organizationId, Guid surveyId, Guid userId, string targetStatus, CampaignTransitionRequest request, string correlationId, CancellationToken ct);
    Task<CampaignCommandResult?> ResendFailuresAsync(Guid organizationId, Guid surveyId, Guid userId, string correlationId, CancellationToken ct);
    Task<IReadOnlyList<CampaignHistoryDto>> HistoryAsync(Guid organizationId, Guid surveyId, CancellationToken ct);
}

public interface IDiagnosticCampaignService {
    Task<IReadOnlyList<DiagnosticCampaignDto>> ListAsync(Guid organizationId, CancellationToken ct);
    Task<DiagnosticCampaignDto?> GetAsync(Guid organizationId, Guid surveyId, CancellationToken ct);
    Task<DiagnosticCampaignDto?> CreateAsync(Guid organizationId, Guid surveyId, Guid userId, CreateCampaignRequest request, string correlationId, CancellationToken ct);
    Task<CampaignCommandResult?> TransitionAsync(Guid organizationId, Guid surveyId, Guid userId, string targetStatus, CampaignTransitionRequest request, string correlationId, CancellationToken ct);
    Task<CampaignCommandResult?> ResendFailuresAsync(Guid organizationId, Guid surveyId, Guid userId, string correlationId, CancellationToken ct);
    Task<IReadOnlyList<CampaignHistoryDto>> HistoryAsync(Guid organizationId, Guid surveyId, CancellationToken ct);
}
