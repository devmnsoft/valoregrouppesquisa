using System.IO;
using Xunit;

namespace Valora.Tests;

public sealed class EmailDeliverabilityStatusTests
{
    [Fact]
    public void DeliverabilityEndpointReturnsOnlyBooleanConfiguration()
    {
        var controller = File.ReadAllText("../../../Valora.Api/Controllers/CommunicationsController.cs");
        Assert.Contains("/admin/email/deliverability/status", controller);
        Assert.Contains("fromEmailConfigured", controller);
        Assert.Contains("smtpConfigured", controller);
        Assert.DoesNotContain("Password =", controller);
    }
}
