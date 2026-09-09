using System.IO;
using Valora.Tests.Support;
using Xunit;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class CertificatePublicValidationTests {
    [Fact]
    public void PublicValidationDoesNotExposeSensitiveTokens() {
        var controller = File.ReadAllText(RepositoryPaths.ApiFile("Controllers", "CertificatesController.cs"));
        Assert.Contains("participantEmailMasked", controller);
        Assert.Contains("/certificates/validate/{certificateCode}", controller);
        Assert.DoesNotContain("resultToken =", controller);
        Assert.DoesNotContain("token_hash", controller);
    }
}
