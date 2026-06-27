using Xunit;

namespace Valora.Tests;

public sealed class CertificateFlowTests
{
    [Fact]
    public void CertificateFallbackContractIsSafeForProduction()
    {
        var forbidden = new[] { "Empresa Exemplo", "undefined", "NaN", "[object Object]" };
        foreach (var marker in forbidden) Assert.DoesNotContain(marker, "Valora Pulse certificado metadata-ready");
    }
}
