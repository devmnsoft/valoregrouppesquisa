using System.IO;
using Valora.Tests.Support;
using Xunit;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class OperationsHealthTests {
    [Fact]
    public void OperationsApiAndPanelArtifactsExist() {
        Assert.Contains("/admin/operations/health", File.ReadAllText(RepositoryPaths.ApiFile("Controllers", "OperationsController.cs")));
        Assert.Contains("status do SMTP", File.ReadAllText(RepositoryPaths.WebFile("Views", "Operations", "Index.cshtml")));
    }
}
