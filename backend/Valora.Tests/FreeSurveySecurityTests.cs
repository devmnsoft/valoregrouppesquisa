using System.IO;
using Xunit;
using Valora.Tests.Support;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class FreeSurveySecurityTests
{
    [Fact]
    public void FreeSurveySecurityArtifactsContainRequiredControls()
    {
        var controller = File.ReadAllText(RepositoryPaths.ApiFile("Controllers", "PublicSurveysController.cs"));
        Assert.Contains("ip-rate-limit", controller);
        Assert.Contains("email-rate-limit", controller);
        Assert.Contains("token-rate-limit", controller);
        Assert.Contains("honeypot-filled", controller);
        Assert.Contains("minimum-fill-time", controller);
        Assert.Contains("duplicate-submission-window", controller);
    }
}
