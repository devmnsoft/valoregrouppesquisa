using Valora.Application.Experience;

namespace Valora.Tests;

public sealed class ExperienceTokenSecurityTests
{
    [Fact]
    public void Generate_ReturnsOpaqueHighEntropyToken()
    {
        var first = ExperienceToken.Generate();
        var second = ExperienceToken.Generate();

        Assert.True(ExperienceToken.IsWellFormed(first));
        Assert.Equal(64, first.Length);
        Assert.NotEqual(first, second);
        Assert.NotEqual(first, ExperienceToken.Hash(first));
    }

    [Theory]
    [InlineData("")]
    [InlineData("token-publico")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void IsWellFormed_RejectsPredictableOrMalformedValues(string token)
    {
        Assert.False(ExperienceToken.IsWellFormed(token));
    }
}
