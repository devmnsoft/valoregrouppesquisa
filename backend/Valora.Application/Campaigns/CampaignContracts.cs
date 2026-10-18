using Valora.Domain.Entities;

namespace Valora.Application.Campaigns;

public static class CanonicalCampaignStatus {
    public const string Draft = "draft";
    public const string Scheduled = "scheduled";
    public const string Active = "active";
    public const string Paused = "paused";
    public const string Closed = "closed";
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(
        [Draft, Scheduled, Active, Paused, Closed, Cancelled],
        StringComparer.OrdinalIgnoreCase);
}

public sealed record CampaignSummaryDto(
    Guid Id,
    Guid OrganizationId,
    Guid SurveyId,
    string SurveyTitle,
    Guid? UnitId,
    string? UnitName,
    Guid? DepartmentId,
    string? DepartmentName,
    string Name,
    string? Audience,
    int ResponseGoal,
    int ActualResponses,
    decimal CompletionRate,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string Status,
    string? PublicUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? LinkStatus = null,
    string? LinkReason = null,
    string? LinkAction = null);

public sealed record CampaignDetailDto(
    Guid Id,
    Guid OrganizationId,
    Guid SurveyId,
    string SurveyTitle,
    Guid? UnitId,
    string? UnitName,
    Guid? DepartmentId,
    string? DepartmentName,
    string Name,
    string? Audience,
    int ResponseGoal,
    int ActualResponses,
    decimal CompletionRate,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string Status,
    string? PublicUrl,
    Guid? CreatedBy,
    string? CreatedByName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? LinkStatus = null,
    string? LinkReason = null,
    string? LinkAction = null);

public sealed record CreateCampaignDto(
    Guid SurveyId,
    string Name,
    Guid? UnitId = null,
    Guid? DepartmentId = null,
    string? Audience = null,
    int ResponseGoal = 0,
    DateTimeOffset? StartsAt = null,
    DateTimeOffset? EndsAt = null);

public sealed record UpdateCampaignDto(
    string Name,
    Guid? UnitId = null,
    Guid? DepartmentId = null,
    string? Audience = null,
    int ResponseGoal = 0,
    DateTimeOffset? StartsAt = null,
    DateTimeOffset? EndsAt = null);

public sealed record CampaignTransitionDto(
    string TargetStatus,
    string? Justification = null);

public sealed record CampaignFilter(
    Guid? SurveyId = null,
    Guid? UnitId = null,
    Guid? DepartmentId = null,
    string? Status = null);

public interface ICampaignRepository {
    Task<IReadOnlyList<CampaignSummaryDto>> ListAsync(Guid organizationId, CampaignFilter? filter, CancellationToken ct);
    Task<CampaignDetailDto?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<CampaignDetailDto> CreateAsync(Campaign campaign, CancellationToken ct);
    Task<CampaignDetailDto?> UpdateAsync(Campaign campaign, CancellationToken ct);
    Task<bool> TransitionStatusAsync(Guid organizationId, Guid id, string expectedStatus, string newStatus, Guid? userId = null, string? justification = null, string? correlationId = null, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken ct);
}

public interface ICampaignService {
    Task<IReadOnlyList<CampaignSummaryDto>> ListAsync(Guid organizationId, CampaignFilter? filter, CancellationToken ct);
    Task<CampaignDetailDto?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<CampaignDetailDto> CreateAsync(Guid organizationId, Guid userId, CreateCampaignDto request, CancellationToken ct);
    Task<CampaignDetailDto?> UpdateAsync(Guid organizationId, Guid id, UpdateCampaignDto request, CancellationToken ct);
    Task<CampaignDetailDto?> TransitionAsync(Guid organizationId, Guid id, string targetStatus, string? justification = null, string? correlationId = null, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken ct);
}
