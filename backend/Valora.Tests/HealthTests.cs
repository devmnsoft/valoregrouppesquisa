using Xunit;

namespace Valora.Tests;

[Trait("Category", "BffIntegration")]
public sealed class HealthTests
{
    [Fact]
    public void WebHealthControllerExists()
    {
        Assert.True(File.Exists(Support.RepositoryPaths.WebFile("Controllers", "WebHealthController.cs")));
    }
}
