namespace Valora.Application.ReadModels;
public sealed record DimensionScoreReadModel(string DimensionName,decimal Score,decimal MaxScore,decimal Percentage,string? LevelLabel);
