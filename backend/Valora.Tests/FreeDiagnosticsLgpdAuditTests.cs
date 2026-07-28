using Valora.Tests.Support;
using Xunit;

[Trait("Category", "Unit")]
public sealed class FreeDiagnosticsLgpdAuditTests
{
    [Fact]
    public void LgpdEventsAreDocumented()
    {
        var documentation = File.ReadAllText(RepositoryPaths.RootFile("FREE_SURVEY_LGPD_AUDIT.md"));
        Assert.Contains("free_survey.email_sent", documentation);
        Assert.Contains("metadata sanitizada", documentation);
    }
}
