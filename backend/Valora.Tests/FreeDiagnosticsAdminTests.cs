using Valora.Tests.Support;
using Xunit;

[Trait("Category", "Unit")]
public sealed class FreeDiagnosticsAdminTests
{
    [Fact]
    public void AdminPanelAndApiContractExist()
    {
        Assert.Contains("/admin/free-diagnostics", File.ReadAllText(RepositoryPaths.ApiFile("Controllers", "FreeDiagnosticsController.cs")));
        Assert.Contains("Diagnósticos gratuitos", File.ReadAllText(RepositoryPaths.WebFile("Views", "FreeDiagnostics", "Index.cshtml")));
    }
}
