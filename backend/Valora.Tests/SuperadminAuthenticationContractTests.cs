using Valora.Tests.Support;

namespace Valora.Tests;

public sealed class SuperadminAuthenticationContractTests
{
    [Fact]
    public void CanonicalBootstrapDoesNotShipDevelopmentAdministratorCredentials()
    {
        var sql = File.ReadAllText(RepositoryPaths.CanonicalDatabaseScript);

        Assert.DoesNotContain("e2e-admin@valoragroup.local", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Valora!12345", sql, StringComparison.Ordinal);
        Assert.Contains("'admin_valora'", sql);
    }

    [Fact]
    public void DevelopmentDiagnosticsIsEnvironmentGatedAndDoesNotExposeSecrets()
    {
        var source = File.ReadAllText(RepositoryPaths.ApiFile("Controllers", "DevelopmentAuthDiagnosticsController.cs"));

        Assert.Contains("if (!environment.IsDevelopment()) return NotFound();", source);
        Assert.Contains("developmentPasswordVerification", source);
        Assert.DoesNotContain("password_hash =", source, StringComparison.OrdinalIgnoreCase);
    }
}
