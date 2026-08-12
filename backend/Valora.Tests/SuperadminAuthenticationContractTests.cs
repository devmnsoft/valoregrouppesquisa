namespace Valora.Tests;

public sealed class SuperadminAuthenticationContractTests
{
    [Fact]
    public void CanonicalBootstrapRepairsDevelopmentSuperadminWithBcryptAndEnterpriseAccess()
    {
        var sql = File.ReadAllText(RepositoryPaths.CanonicalDatabaseScript);

        Assert.Contains("'e2e-admin@valoragroup.local','Super Administrador Valora'", sql);
        Assert.Contains("public.crypt('Valora!12345',public.gen_salt('bf',12))", sql);
        Assert.Contains("WHERE r.code='admin_valora' AND r.deleted_at IS NULL", sql);
        Assert.Contains("JOIN valorapesquisa.plans p ON p.code='enterprise'", sql);
        Assert.Contains("SET name='Administrador Valora',is_system=true,deleted_at=NULL", sql);
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
