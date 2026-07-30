using Xunit;
using Valora.Tests.Support;

namespace Valora.Tests;
[Trait("Category", "StaticContract")]
public sealed class OperationalStaticContractTests
{
    [Fact] public void Operational_contracts_do_not_expose_sensitive_fields()
    {
        var dtoDirectory = RepositoryPaths.BackendFile("Valora.Application", "DTOs");
        var dto = string.Join('\n', Directory.EnumerateFiles(dtoDirectory, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
        Assert.DoesNotContain("password_hash", dto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token_hash", dto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("result_token_hash", dto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("smtp_password", dto, StringComparison.OrdinalIgnoreCase);
    }
    [Fact] public void Reports_certificates_exports_lgpd_email_are_declared()
    {
        var services = File.ReadAllText(RepositoryPaths.BackendFile("Valora.Application", "Services", "OperationalFeatureServices.cs"));
        Assert.Contains("ReportService", services);
        Assert.Contains("CertificateOperationalService", services);
        Assert.Contains("ExportService", services);
        Assert.Contains("LgpdConsentService", services);
        Assert.Contains("EmailQueueService", services);
    }
}
