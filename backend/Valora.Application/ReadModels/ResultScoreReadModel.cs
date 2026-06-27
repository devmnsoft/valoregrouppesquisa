namespace Valora.Application.ReadModels;
public sealed record ResultScoreReadModel(Guid ResponseId,decimal TotalScore,decimal MaxScore,decimal Percentage,string MaturityLabel,string? RadarText,string? StrategicTruth,string? RiskIfNothingChanges,string? NextLevel);
