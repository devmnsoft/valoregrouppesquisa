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

    [Fact]
    public void Administration_control_center_has_tenant_safe_schema_and_permissions()
    {
        var sql = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/database/postgresql/migrations/2026_08_saas_administration_control_center.sql"));
        foreach (var table in new[] { "saas_customers", "saas_customer_contacts", "saas_customer_users", "saas_customer_user_profiles",
                     "saas_profile_permissions", "saas_customer_modules", "saas_customer_feature_flags", "saas_customer_plan_limits",
                     "saas_customer_billing_accounts", "saas_billing_invoices", "saas_billing_invoice_items", "saas_payment_records",
                     "saas_access_blocks", "saas_admin_actions", "saas_customer_audit_events", "saas_login_identifiers" })
        {
            Assert.Contains($"CREATE TABLE IF NOT EXISTS valorapesquisa.{table}", sql);
        }

        foreach (var permission in new[] { "saas_admin.view", "saas_admin.manage", "saas_customers.view", "saas_customers.manage",
                     "saas_customers.block", "saas_users.manage", "saas_users.block", "saas_modules.manage", "saas_billing.view",
                     "saas_billing.manage", "saas_impersonation.use", "organization_users.manage", "organization_profiles.manage" })
            Assert.True(Valora.Application.Access.ValoraAccessCatalog.IsCanonicalPermission(permission), permission);

        Assert.DoesNotContain("SELECT *", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("numeric(14,2)", sql);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
