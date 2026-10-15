using System.Text.Json;
using Valora.Application.Evolution;

namespace Valora.Tests;

public sealed class EvolutionPreservedMetadataTests {
    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"Score\":\"invalid\"}")]
    [InlineData("{\"Score\":0,\"ReferenceAt\":\"2026-10-01\",\"Basis\":null}")]
    public void Invalid_historical_metadata_is_unavailable_instead_of_a_fabricated_measurement(string json) {
        Assert.Null(EvolutionPreservedMetadata.Read(json));
        Assert.Null(EvolutionPreservedMetadata.Read(json, "Baseline"));
    }

    [Fact]
    public void Valid_zero_is_preserved_and_one_broken_side_does_not_hide_the_other() {
        var measurement = new PreservedEvolutionMeasurement(Guid.NewGuid(), Guid.NewGuid(), "Preserved", 0,
            DateTime.UtcNow, 4, new("v1", "people", "1-5", "weighted", "all", true), "{}");
        var json = JsonSerializer.Serialize(new { Baseline = measurement, Current = new { Score = 80 } });
        Assert.Equal(measurement, EvolutionPreservedMetadata.Read(json, "Baseline"));
        Assert.Null(EvolutionPreservedMetadata.Read(json, "Current"));
    }
}
