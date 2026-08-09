using Valora.Tests.Support;
using Xunit;

[Trait("Category", "Unit")]
public sealed class CertificateRichContentTests
{
    [Fact]
    public void Sprint46ContractIsDocumented() => Assert.True(File.Exists(RepositoryPaths.RootFile("SPRINT_46_FREE_DIAGNOSTIC_E2E_AUDIT.md")));

    [Fact]
    public void PublicCertificateContainsIdentityResultAndValidationData()
    {
        var source = File.ReadAllText(RepositoryPaths.ApplicationFile("Certificates", "CertificateService.cs"));

        Assert.Contains("Valora Insight™", source);
        Assert.Contains("Valora Group", source);
        Assert.Contains("data.Result.Percentage", source);
        Assert.Contains("data.Result.MaturityLabel", source);
        Assert.Contains("data.Certificate.CertificateCode", source);
        Assert.Contains("/certificado/validar/", source);
        Assert.DoesNotContain("token=seu-token", source);
    }

    [Fact]
    public void CertificatePdfHasPremiumVisualCommandsAndAValidTrailer()
    {
        var source = File.ReadAllText(RepositoryPaths.ApplicationFile("Certificates", "CertificateService.cs"));

        Assert.Contains("0.063 0.184 0.212 rg", source);
        Assert.Contains("0.725 0.592 0.294 RG", source);
        Assert.Contains("%PDF-1.4", source);
        Assert.Contains("%%EOF", source);
    }
}
