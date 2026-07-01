namespace Valora.Tests;
public sealed class OperationalStaticContractTests
{
    [Fact] public void Operational_contracts_do_not_expose_sensitive_fields()
    {
        var dto = File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"../../../../Valora.Application/DTOs/OperationalDtos.cs"));
        Assert.DoesNotContain("password_hash", dto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token_hash", dto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("result_token_hash", dto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("smtp_password", dto, StringComparison.OrdinalIgnoreCase);
    }
    [Fact] public void Reports_certificates_exports_lgpd_email_are_declared()
    {
        var root = Path.Combine(AppContext.BaseDirectory,"../../../..");
        Assert.Contains("ReportService", File.ReadAllText(Path.Combine(root,"Valora.Application/Services/OperationalServices.cs")));
        Assert.Contains("CertificateOperationalService", File.ReadAllText(Path.Combine(root,"Valora.Application/Services/OperationalServices.cs")));
        Assert.Contains("ExportService", File.ReadAllText(Path.Combine(root,"Valora.Application/Services/OperationalServices.cs")));
        Assert.Contains("LgpdConsentService", File.ReadAllText(Path.Combine(root,"Valora.Application/Services/OperationalServices.cs")));
        Assert.Contains("EmailQueueService", File.ReadAllText(Path.Combine(root,"Valora.Application/Services/OperationalServices.cs")));
    }
}
