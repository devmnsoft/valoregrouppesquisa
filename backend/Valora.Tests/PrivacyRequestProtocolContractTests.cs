using Xunit;
using Valora.Tests.Support;

namespace Valora.Tests;

[Trait("Category", "Unit")]
public sealed class PrivacyRequestProtocolContractTests
{
    [Fact]
    public void Privacy_requests_use_public_protocol_not_raw_identifier()
    {
        var dto = File.ReadAllText(RepositoryPaths.ApplicationFile("DTOs", "OperationalDtos.cs"));
        var controller = File.ReadAllText(RepositoryPaths.ApiFile("Controllers", "LgpdController.cs"));
        var sql = File.ReadAllText(RepositoryPaths.BackendFile("database", "postgresql", "050_reports_certificates_exports_lgpd_email.sql"));

        Assert.Contains("string Protocol", dto);
        Assert.Contains("/public/lgpd/requests/{protocol}", controller);
        Assert.DoesNotContain("/public/lgpd/requests/{protocol:guid}", controller);
        Assert.Contains("protocol text NOT NULL", sql);
        Assert.Contains("idx_privacy_requests_protocol", sql);
    }
}
