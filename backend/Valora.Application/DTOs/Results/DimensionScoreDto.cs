namespace Valora.Application.DTOs;
public sealed record DimensionScoreDto(string DimensionName,decimal Score,decimal MaxScore,decimal Percentage,string? LevelLabel);
