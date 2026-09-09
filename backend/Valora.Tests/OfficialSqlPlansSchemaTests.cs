using Xunit;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class OfficialSqlPlansSchemaTests
{
    [Fact]
    public void OfficialPlanSeedsUseCanonicalColumnsAndKeepLegacyConvergenceExplicit()
    {
        var sql = File.ReadAllText(Support.RepositoryPaths.CanonicalDatabaseScript);
        Assert.DoesNotContain("price_label", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("capability_level", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("capability_type", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("organizations(name,public_name,slug,status,plan_id)", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("plan_capabilities(plan_id,capability,capability_code,capability_key,enabled,is_enabled)", sql);
        Assert.Matches(@"(?is)ON CONFLICT\s*\(\s*plan_id\s*,\s*capability_key\s*\)\s*DO UPDATE", sql);
    }

    [Fact]
    public void OfficialPlanSeedUsesCodeBasedIdempotentShape()
    {
        var sql = File.ReadAllText(Support.RepositoryPaths.CanonicalDatabaseScript);
        Assert.Contains("INSERT INTO valorapesquisa.plans(code", sql);
        Assert.Contains("ON CONFLICT (code) DO UPDATE", sql);
        Assert.Contains("JOIN (VALUES", sql);
        Assert.Matches(@"(?is)ON CONFLICT\s*\(\s*plan_id\s*,\s*capability_key\s*\)\s*DO UPDATE", sql);
    }
}
