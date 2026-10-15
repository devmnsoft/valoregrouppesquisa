using Valora.Application.Evolution;

namespace Valora.Tests;

public sealed class EvolutionReevaluationTests {
    private static PreservedEvolutionMeasurement Measurement(decimal score, DateTime reference, string source) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Research", score, reference, 4,
            new("professional@3", "people", "1-5", "weighted", "cohort-a", true), source);

    [Fact]
    public void EarlierOrEqualReferenceCannotBecomePosteriorMeasurement() {
        var initial = Measurement(60, DateTime.UtcNow, "initial");
        Assert.Throws<ArgumentException>(() => EvolutionReevaluation.Preview(1, initial, Measurement(70, initial.ReferenceAt, "posterior")));
        Assert.Throws<ArgumentException>(() => EvolutionReevaluation.Preview(1, initial, Measurement(70, initial.ReferenceAt.AddDays(-1), "posterior")));
    }

    [Fact]
    public void PreviewPreservesBothReadingsAndSignedPointsWithoutPercentageOrPrediction() {
        var initial = Measurement(60, DateTime.UtcNow, "initial");
        var posterior = Measurement(50, initial.ReferenceAt.AddDays(1), "posterior");
        var preview = EvolutionReevaluation.Preview(2, initial, posterior);
        Assert.Equal(initial, preview.Baseline);
        Assert.Equal(posterior, preview.Current);
        Assert.Equal(-10, preview.Comparison.AbsoluteVariation);
        Assert.Equal(preview.Fingerprint, EvolutionReevaluation.Preview(2, initial, posterior with { Id = Guid.NewGuid() }).Fingerprint);
        Assert.NotEqual(preview.Fingerprint, EvolutionReevaluation.Preview(3, initial, posterior).Fingerprint);
        Assert.NotEqual(preview.Fingerprint, EvolutionReevaluation.Preview(2, initial, posterior with { SourceJson = "changed" }).Fingerprint);
    }
}
