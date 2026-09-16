using Valora.Application.Evolution;
using Xunit;

namespace Valora.Tests;

public sealed class EvolutionComparisonTests {
    private static EvolutionComparisonBasis Basis(string version="v1",string dimensions="people|process",string scale="0-100",string criteria="weighted-v1",string population="all",bool available=true)
        =>new(version,dimensions,scale,criteria,population,available);

    [Fact]
    public void Compare_returns_absolute_variation_only_for_equivalent_bases() {
        var comparison=EvolutionComparisonService.Compare(62m,Basis(),68.5m,Basis());
        Assert.True(comparison.IsComparable);
        Assert.Equal(6.5m,comparison.AbsoluteVariation);
        Assert.Contains("não demonstra causalidade",comparison.Limitation);
    }

    [Theory]
    [InlineData("methodology")]
    [InlineData("dimensions")]
    [InlineData("scale")]
    [InlineData("criteria")]
    [InlineData("population")]
    public void Compare_does_not_invent_variation_when_a_required_basis_differs(string field) {
        var current=field switch {
            "methodology"=>Basis(version:"v2"), "dimensions"=>Basis(dimensions:"people"),
            "scale"=>Basis(scale:"1-5"), "criteria"=>Basis(criteria:"weighted-v2"),
            _=>Basis(population:"leaders") };
        var comparison=EvolutionComparisonService.Compare(62m,Basis(),70m,current);
        Assert.False(comparison.IsComparable);
        Assert.Null(comparison.AbsoluteVariation);
        Assert.Contains("separadamente",comparison.Limitation);
    }

    [Fact]
    public void Compare_requires_available_scores() {
        var comparison=EvolutionComparisonService.Compare(null,Basis(available:false),70m,Basis());
        Assert.False(comparison.IsComparable);
        Assert.Null(comparison.AbsoluteVariation);
    }
}
