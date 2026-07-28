using System.IO;
using Xunit;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class CertificatePublicValidationTests
{
    [Fact]
    public void PublicValidationDoesNotExposeSensitiveTokens()
    {
        var controller = File.ReadAllText("../../../Valora.Api/Controllers/CertificatesController.cs");
        Assert.Contains("participantEmailMasked", controller);
        Assert.Contains("/certificates/validate/{certificateCode}", controller);
        Assert.DoesNotContain("resultToken =", controller);
        Assert.DoesNotContain("token_hash", controller);
    }
}
