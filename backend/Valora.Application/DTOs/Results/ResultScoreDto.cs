namespace Valora.Application.DTOs;
public sealed record ResultScoreDto(decimal TotalScore,decimal MaxScore,decimal Percentage,string MaturityLabel,string? StrongestDimension,string? WeakestDimension,string? RadarText,string? StrategicTruth,string? RiskIfNoAction,string? NextLevelRecommendation);
