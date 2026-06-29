using System.IO;
using Xunit;

namespace Valora.Tests;

public sealed class OperationsHealthTests
{
    [Fact]
    public void OperationsApiAndPanelArtifactsExist()
    {
        Assert.Contains("/admin/operations/health", File.ReadAllText("../../../Valora.Api/Controllers/OperationsController.cs"));
        Assert.Contains("status do SMTP", File.ReadAllText("../../../Valora.Web/Views/Operations/Index.cshtml"));
    }
}
