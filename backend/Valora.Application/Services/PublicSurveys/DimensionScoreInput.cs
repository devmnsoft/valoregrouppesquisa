namespace Valora.Application.Services;
public sealed record DimensionScoreInput(string DimensionName,decimal Score,decimal MaxScore,decimal Percentage,string? LevelLabel);
