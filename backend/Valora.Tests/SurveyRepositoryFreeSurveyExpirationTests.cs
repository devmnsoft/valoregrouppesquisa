using Xunit;

namespace Valora.Tests;

public sealed class SurveyRepositoryFreeSurveyExpirationTests
{
    [Fact]
    public void RepositoryKeepsOfficialFreeSurveyAvailableWhenExpiresAtIsPast()
    {
        var source = File.ReadAllText(Path.Combine("..", "..", "..", "..", "backend", "Valora.Infrastructure", "Repositories", "SurveyRepository.cs"));
        Assert.Contains("IsFreeOfficialSurvey", source);
        Assert.Contains("FreeOfficialSql", source);
        Assert.Contains("s.expires_at IS NULL OR s.expires_at>now() OR", source);
        Assert.Contains("sl.expires_at IS NULL OR sl.expires_at>now() OR", source);
        Assert.Contains("sl.revoked_at IS NULL", source);
    }
}
