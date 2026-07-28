using Valora.Tests.Support;
using Xunit;

[Trait("Category", "Unit")]
public sealed class FreeDiagnosticsEmailResendTests
{
    [Fact]
    public void ResendLimitAndAuditAreDeclared()
    {
        var source = File.ReadAllText(RepositoryPaths.ApplicationFile("FreeDiagnostics", "FreeDiagnosticsAppService.cs"));
        Assert.Contains(">= 3", source);
        Assert.Contains("free_survey.result_email_resent", source);
    }
}
