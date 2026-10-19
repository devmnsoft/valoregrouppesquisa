namespace Valora.Web.Models.ViewModels;

public sealed record SurveyOptionItem(
    Guid Id,
    string Title,
    string Status,
    string? FormName,
    int CompletedResponses);

public sealed record PublicLinkItemViewModel(
    Guid Id,
    Guid SurveyId,
    string PublicUrl,
    string Status,
    DateTimeOffset? StartsAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt) {
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;
    public bool IsActive => string.Equals(Status, "active", StringComparison.OrdinalIgnoreCase) && !IsExpired;

    public string StatusBadgeClass => Status.ToLowerInvariant() switch {
        "active" when !IsExpired => "bg-success text-white",
        "active" when IsExpired => "bg-warning text-dark",
        "revoked" => "bg-danger text-white",
        "inactive" => "bg-secondary text-white",
        _ => "bg-secondary text-white"
    };

    public string StatusDisplayName => Status.ToLowerInvariant() switch {
        "active" when !IsExpired => "Ativo",
        "active" when IsExpired => "Expirado",
        "revoked" => "Revogado",
        "inactive" => "Inativo",
        _ => Status
    };
}

public sealed record SurveysPublicLinksViewModel(
    IReadOnlyList<SurveyOptionItem> AvailableSurveys,
    Guid? SelectedSurveyId,
    string? SelectedSurveyTitle,
    string? SelectedSurveyStatus,
    IReadOnlyList<PublicLinkItemViewModel> Links,
    int TotalSurveys,
    int TotalLinks,
    int ActiveLinks,
    int ExpiredOrRevokedLinks);
