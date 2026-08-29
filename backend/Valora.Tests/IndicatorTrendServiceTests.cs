using Valora.Application.Indicators;
using Xunit;

namespace Valora.Tests;

public sealed class IndicatorTrendServiceTests
{
    private static IndicatorMeasurementDto Measurement(decimal value, int day) =>
        new(Guid.NewGuid(), Guid.NewGuid(), value, new DateTime(2026, 1, day), "Fonte auditável", Guid.NewGuid(), "Medição manual justificada", 1, null);

    [Fact]
    public void Less_than_two_measurements_is_insufficient_data()
    {
        var result = new IndicatorTrendService().Calculate([Measurement(10, 1)]);
        Assert.Equal(IndicatorTrend.InsufficientData, result.Trend);
        Assert.Contains("insuficientes", result.Limitation);
    }

    [Theory]
    [InlineData("higher_is_better", IndicatorTrend.Improving)]
    [InlineData("lower_is_better", IndicatorTrend.Worsening)]
    public void Comparison_rule_controls_trend(string rule, IndicatorTrend expected)
    {
        var result = new IndicatorTrendService().Calculate([Measurement(10, 1), Measurement(12, 2)], rule);
        Assert.Equal(expected, result.Trend);
        Assert.Equal(2, result.SampleSize);
        Assert.Contains("não demonstra causalidade", result.Limitation);
    }
}
