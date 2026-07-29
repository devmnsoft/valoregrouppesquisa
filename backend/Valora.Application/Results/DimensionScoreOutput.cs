namespace Valora.Application.Results;

public sealed record DimensionScoreOutput(string Dimension, decimal RawScore, decimal MaxScore, decimal Percentage, decimal Normalized5);
