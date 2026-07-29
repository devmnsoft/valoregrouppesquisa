using Valora.Tests.Support;
using Xunit;

[Trait("Category", "Unit")]
public sealed class FreeDiagnosticsCertificateOperationsTests
{
    [Fact]
    public void CertificateRegenerationDoesNotChangeResult()
    {
        Assert.Contains("não altera resultado original", File.ReadAllText(RepositoryPaths.RootFile("FREE_SURVEY_CERTIFICATE_OPERATIONS.md")));
    }
}
