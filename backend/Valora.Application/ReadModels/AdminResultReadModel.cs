namespace Valora.Application.ReadModels;

public sealed record AdminResultReadModel(
    Guid ResponseId,
    Guid OrganizationId,
    string OrganizationName,
    Guid SurveyId,
    string SurveyTitle,
    DateTimeOffset? PeriodStart,
    DateTimeOffset? PeriodEnd,
    Guid FormId,
    string FormName,
    Guid FormVersionId,
    int FormVersion,
    Guid? ResultId,
    string ProcessingStatus,
    DateTimeOffset? ProcessedAt,
    DateTimeOffset? CompletedAt,
    decimal? TotalScore,
    decimal? MaxScore,
    decimal? Percentage,
    string? MaturityLabel,
    string? RadarText,
    string? StrategicTruth,
    string? RiskIfNothingChanges,
    string? NextLevel,
    int EligibleResponseCount,
    IReadOnlyList<AdminResultDimensionReadModel> Dimensions,
    IReadOnlyList<AdminResultReportReadModel> Reports,
    IReadOnlyList<AdminResultPlanReadModel> Plans);

public sealed record AdminResultDimensionReadModel(string DimensionName, decimal? Score, decimal? MaxScore, decimal? Percentage, string? LevelLabel);
public sealed record AdminResultReportReadModel(Guid Id, string Title, string Format, string Status, string? FileName, string? MimeType, DateTimeOffset CreatedAt);
public sealed record AdminResultPlanReadModel(Guid Id, string Title, string Status, string Priority, string? OwnerName, DateTimeOffset? DueAt, int ProgressPercent);
