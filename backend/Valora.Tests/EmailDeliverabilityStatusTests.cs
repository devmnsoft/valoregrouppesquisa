using System.IO;
using Xunit;
using Valora.Tests.Support;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class EmailDeliverabilityStatusTests
{
    [Fact]
    public void DeliverabilityEndpointReturnsOnlyBooleanConfiguration()
    {
        var controller = File.ReadAllText(RepositoryPaths.ApiFile("Controllers", "CommunicationsController.cs"));
        Assert.Contains("/admin/email/deliverability/status", controller);
        Assert.Contains("fromEmailConfigured", controller);
        Assert.Contains("smtpConfigured", controller);
        Assert.DoesNotContain("Password =", controller);
    }
}
