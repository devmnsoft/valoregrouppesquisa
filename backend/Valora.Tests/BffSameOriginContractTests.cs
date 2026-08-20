using Valora.Tests.Support;

namespace Valora.Tests;

public sealed class BffSameOriginContractTests
{
    [Fact]
    public void BrowserConfiguration_DoesNotExposeInternalApiOrigin()
    {
        var source = Read("backend", "Valora.Web", "Controllers", "WebConfigController.cs");
        Assert.DoesNotContain("API_BASE_URL =", source, StringComparison.Ordinal);
        Assert.Contains("BFF_BASE_URL = string.Empty", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AjaxClient_RejectsExternalAndNonBffPaths()
    {
        var source = Read("backend", "Valora.Web", "wwwroot", "js", "api", "ajax-client.js");
        Assert.DoesNotContain("API_BASE_URL", source, StringComparison.Ordinal);
        Assert.Contains("path.startsWith('/bff/')", source, StringComparison.Ordinal);
        Assert.Contains("parsed.origin !== window.location.origin", source, StringComparison.Ordinal);
        Assert.Contains("credentials: 'same-origin'", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DevelopmentCors_UsesExactWebOriginsWithoutWildcard()
    {
        var source = Read("backend", "Valora.Api", "Configuration", "ApiServiceCollectionExtensions.cs");
        Assert.Contains("https://localhost:7088", source, StringComparison.Ordinal);
        Assert.Contains("http://localhost:5088", source, StringComparison.Ordinal);
        Assert.Contains("https://127.0.0.1:7088", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowAnyOrigin", source, StringComparison.Ordinal);
    }

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([RepositoryPaths.RepositoryRoot, .. parts]));
}
