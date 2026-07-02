using Xunit;

namespace Valora.Tests;

public sealed class PrivacyRequestProtocolContractTests
{
    [Fact]
    public void Privacy_requests_use_public_protocol_not_raw_identifier()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        var dto = File.ReadAllText(Path.Combine(root, "Valora.Application/DTOs/OperationalDtos.cs"));
        var controller = File.ReadAllText(Path.Combine(root, "Valora.Api/Controllers/LgpdController.cs"));
        var sql = File.ReadAllText(Path.Combine(root, "../database/postgresql/050_reports_certificates_exports_lgpd_email.sql"));

        Assert.Contains("string Protocol", dto);
        Assert.Contains("/public/lgpd/requests/{protocol}", controller);
        Assert.DoesNotContain("/public/lgpd/requests/{protocol:guid}", controller);
        Assert.Contains("protocol text NOT NULL", sql);
        Assert.Contains("idx_privacy_requests_protocol", sql);
    }
}
