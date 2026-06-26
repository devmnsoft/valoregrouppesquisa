namespace Valora.Application.Results;

public sealed record ValoraInsightResult(decimal TotalScore, decimal MaxScore, string Level, object Radar, string StrategicTruth, string Risk, string NextLevel);
