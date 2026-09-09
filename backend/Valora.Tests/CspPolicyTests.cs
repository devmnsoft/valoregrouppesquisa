using Valora.Tests.Support;
using Xunit;

[Trait("Category", "StaticContract")]
public sealed class CspPolicyTests
{
    [Fact]
    public void OfficialWebRuntimeUsesOnlySameOriginAssets()
    {
        var program = File.ReadAllText(RepositoryPaths.WebFile("Program.cs"));
        var layout = File.ReadAllText(RepositoryPaths.WebFile("Views", "Shared", "_Layout.cshtml"));
        Assert.Contains("Content-Security-Policy", program);
        Assert.DoesNotContain("https://cdn.jsdelivr.net", layout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("src=\"http", layout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=\"http", layout, StringComparison.OrdinalIgnoreCase);
    }
}
