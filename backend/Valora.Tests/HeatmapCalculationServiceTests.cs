using Valora.Application.Heatmap;

namespace Valora.Tests;

public sealed class HeatmapCalculationServiceTests
{
    [Theory]
    [InlineData(90, "excelente")]
    [InlineData(70, "saudável")]
    [InlineData(55, "em atenção")]
    [InlineData(40, "crítico")]
    [InlineData(20, "muito crítico")]
    public void Level_uses_official_visual_scale(decimal score, string expected) =>
        Assert.Equal(expected, HeatmapCalculationService.Level(score, HeatmapCalculationService.MinimumSample));

    [Fact]
    public void Small_sample_is_never_classified_as_performance() =>
    {
        Assert.Equal("amostra insuficiente", HeatmapCalculationService.Level(100, HeatmapCalculationService.MinimumSample - 1));
        Assert.Equal("indeterminado", HeatmapCalculationService.Risk(0, HeatmapCalculationService.MinimumSample - 1));
    }
}
