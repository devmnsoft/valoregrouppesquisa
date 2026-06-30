using Xunit;

namespace Valora.Tests;

public sealed class ApiParityTests
{
    [Fact]
    public void ApiHasSprint64ControllerSurface()
    {
        var controllersDir = Path.Combine("..", "..", "..", "..", "backend", "Valora.Api", "Controllers");
        var all = string.Join('\n', Directory.EnumerateFiles(controllersDir, "*.cs").Select(File.ReadAllText));
        foreach (var name in new[] { "AuthController", "OrganizationsController", "PlansController", "SurveysController", "ResponsesController", "CertificatesController", "CommunicationsController", "OperationsController" })
            Assert.Contains(name, all);
    }
}
