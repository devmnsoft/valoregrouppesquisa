using Valora.Domain.ValueObjects;
using Valora.Tests.Support;

namespace Valora.Tests;

public sealed class ModularSaasFoundationTests {
    [Fact]
    public void MigrationDefinesTheCanonicalModularSaasContract() {
        var sql = File.ReadAllText(RepositoryPaths.CanonicalDatabaseScript);
        foreach (var table in new[]
        {
            "saas_modules", "saas_module_features", "saas_module_prices", "saas_plans", "saas_plan_modules",
            "client_subscriptions", "client_subscription_modules", "client_module_usage", "client_users",
            "client_user_profiles", "client_profiles", "client_profile_permissions", "module_access_audit",
            "subscription_audit_events"
        }) Assert.Contains($"CREATE TABLE IF NOT EXISTS valorapesquisa.{table}", sql);

        foreach (var module in new[]
        {
            "diagnostics", "forms", "surveys", "results", "reports", "certificates", "action_center",
            "evolution", "journey", "indicators", "benchmarks", "methodology_studio", "valora_ai", "data_hub",
            "governance", "security_compliance", "administration", "subscriptions", "success_center"
        }) Assert.Contains($"('{module}'", sql);

        Assert.Contains("ON CONFLICT", sql);
        Assert.Contains("module_access_audit", sql);
        Assert.Contains("subscription_audit_events", sql);
    }

    [Fact]
    public void DirectModuleAccessIsProtectedAndPlatformAdministratorBypassesTenantContract() {
        var middleware = File.ReadAllText(RepositoryPaths.WebFile("Security", "CommercialModuleAccessMiddleware.cs"));
        var repository = File.ReadAllText(Path.Combine(RepositoryPaths.RepositoryRoot, "backend",
            "Valora.Infrastructure", "ModularSaas", "CommercialSaasRepository.cs"));
        Assert.Contains("MODULE_NOT_CONTRACTED", repository);
        Assert.True(repository.IndexOf("MODULE_NOT_CONTRACTED", StringComparison.Ordinal) <
                    repository.IndexOf("SUBSCRIPTION_INACTIVE", StringComparison.Ordinal));
        Assert.Contains("IsPlatformAdministrator", middleware);
        Assert.Contains("/Modules?blocked=", middleware);
        Assert.Contains("organization_id", middleware);
        Assert.Contains("CommercialModuleAccessMiddleware", File.ReadAllText(RepositoryPaths.WebFile("Program.cs")));
    }

    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("52998224725", "52998224725")]
    public void CpfIsValidatedAndNormalized(string input, string expected) {
        Assert.True(Cpf.TryCreate(input, out var cpf));
        Assert.Equal(expected, cpf!.Value);
    }

    [Theory]
    [InlineData("111.111.111-11")]
    [InlineData("123")]
    public void InvalidCpfIsRejected(string input) => Assert.False(Cpf.TryCreate(input, out _));

    [Fact]
    public void LoginAcceptsEmailCpfAndCnpjWithoutLoggingTheRawIdentifier() {
        var auth = File.ReadAllText(RepositoryPaths.ApplicationFile("Services", "Auth", "AuthService.cs"));
        var repository = File.ReadAllText(RepositoryPaths.InfrastructureFile("Repositories", "UserRepository.cs"));
        var view = File.ReadAllText(RepositoryPaths.WebFile("Views", "Account", "Login.cshtml"));
        Assert.Contains("NormalizeLoginIdentifier", auth);
        Assert.Contains("Cpf.TryCreate", auth);
        Assert.Contains("Cnpj.TryCreate", auth);
        Assert.Contains("GetByLoginAsync", repository);
        Assert.Contains("IdentifierHash", repository);
        Assert.Contains("CPF, CNPJ ou e-mail institucional", view);
    }

    [Fact]
    public void MigrationRunnerUsesCanonicalVersionChecksumsAndBootstrapOrdering() {
        var runner = File.ReadAllText(RepositoryPaths.InfrastructureFile("Database", "MigrationRunner.cs"));
        Assert.Contains("SELECT version FROM valorapesquisa.schema_migrations", runner);
        Assert.Contains("SHA256.HashData", runner);
        Assert.Contains("BootstrapVersion", runner);
        Assert.Contains("script_completo.sql", runner);
    }

    [Fact]
    public void MarketplaceAndSuperadminSurfacesAreRealRazorRoutes() {
        var marketplace = File.ReadAllText(RepositoryPaths.WebFile("Views", "Saas", "Marketplace.cshtml"));
        var admin = File.ReadAllText(RepositoryPaths.WebFile("Controllers", "SaasAdminController.cs"));
        Assert.Contains("Solicitar upgrade", marketplace);
        Assert.Contains("Authorize(Roles = \"admin_valora\")", admin);
        Assert.Contains("HttpGet(\"SaaS\")", admin);
        Assert.Contains("HttpPost(\"Clients/{id:guid}/Modules\")", admin);
    }
}
