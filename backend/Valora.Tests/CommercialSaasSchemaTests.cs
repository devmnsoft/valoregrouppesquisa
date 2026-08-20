namespace Valora.Tests;

public sealed class CommercialSaasSchemaTests
{
    [Fact]
    public void Migration_contains_complete_idempotent_billing_contract()
    {
        var sql = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/database/postgresql/migrations/2026_08_commercial_saas_layer.sql"));
        foreach (var table in new[] { "subscription_usage", "invoices", "invoice_items", "payments", "billing_ledger" })
            Assert.Contains($"CREATE TABLE IF NOT EXISTS valorapesquisa.{table}", sql);
        Assert.Contains("ON CONFLICT (code) DO UPDATE", sql);
        Assert.Contains("admin_valora", sql);
    }

    [Fact]
    public void Canonical_catalog_contains_commercial_permissions()
    {
        foreach (var permission in new[] { "plans.read", "plans.manage", "subscriptions.read", "subscriptions.manage",
                     "billing.read", "billing.manage", "usage.read", "usage.manage", "upgrades.manage" })
            Assert.True(Valora.Application.Access.ValoraAccessCatalog.IsCanonicalPermission(permission), permission);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
