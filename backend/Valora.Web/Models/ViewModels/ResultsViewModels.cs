namespace Valora.Web.Models.ViewModels;

public sealed record ResultItemViewModel(
    Guid ResponseId,
    Guid? SurveyId,
    string SurveyTitle,
    string ParticipantMasked,
    string ParticipantEmailMasked,
    decimal? TotalScore,
    decimal? MaxScore,
    decimal? Percentage,
    string MaturityLabel,
    string? RadarSummary,
    DateTimeOffset CompletedAt,
    bool HasSufficiency
);

public sealed record ResultsIndexViewModel(
    IReadOnlyList<ResultItemViewModel> Items,
    int TotalResults,
    decimal AveragePercentage,
    int SufficientCount,
    int ReadyForReportCount,
    string? Search,
    string? MaturityFilter
);
