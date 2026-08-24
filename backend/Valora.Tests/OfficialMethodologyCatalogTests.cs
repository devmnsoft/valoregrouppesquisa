using Valora.Domain.Methodology;

namespace Valora.Tests;

public sealed class OfficialMethodologyCatalogTests
{
    [Fact]
    public void Publishes_the_twelve_official_indices_without_duplicate_codes()
    {
        Assert.Equal(12, ValoraIndexCatalog.All.Count);
        Assert.Equal(12, ValoraIndexCatalog.All.Select(index => index.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(new[] { "IMO", "ICS", "IIO", "IGO", "ICO", "ILI", "IPO", "IDO", "IAC", "IAR", "IIS", "ISO" },
            code => Assert.True(ValoraIndexCatalog.IsOfficial(code)));
    }

    [Theory]
    [InlineData(0, OrganizationalMaturityLevel.Initial)]
    [InlineData(25, OrganizationalMaturityLevel.Initial)]
    [InlineData(26, OrganizationalMaturityLevel.Structuring)]
    [InlineData(50, OrganizationalMaturityLevel.Structuring)]
    [InlineData(51, OrganizationalMaturityLevel.Integrated)]
    [InlineData(75, OrganizationalMaturityLevel.Integrated)]
    [InlineData(76, OrganizationalMaturityLevel.Mature)]
    [InlineData(100, OrganizationalMaturityLevel.Mature)]
    public void Applies_the_official_maturity_boundaries(int score, OrganizationalMaturityLevel expected) =>
        Assert.Equal(expected, OrganizationalMaturity.Classify(score));

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Rejects_scores_outside_the_official_scale(int score) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => OrganizationalMaturity.Classify(score));
}
