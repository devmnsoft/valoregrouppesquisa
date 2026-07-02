using Xunit;

namespace Valora.Tests;

public sealed class OfficialSqlPlansSchemaTests
{
    private static string Root => Path.Combine("..", "..", "..", "..");

    [Theory]
    [InlineData("scriptbd_completo.sql")]
    [InlineData("database/postgresql/scriptbd_completo.sql")]
    [InlineData("database/postgresql/011_seed_official_plans.sql")]
    public void OfficialPlanSeedsDoNotReferenceKnownRemovedColumns(string relativePath)
    {
        var sql = File.ReadAllText(Path.Combine(Root, relativePath));
        Assert.DoesNotContain("price_label", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("plan_limits(plan_id,limit_key,limit_value)", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("capability_key", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("capability_level", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("capability_type", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("organizations(name,public_name,slug,status,plan_id)", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OfficialPlanSeedUsesCodeBasedIdempotentShape()
    {
        var sql = File.ReadAllText(Path.Combine(Root, "database/postgresql/011_seed_official_plans.sql"));
        Assert.Contains("INSERT INTO valorapesquisa.plans(code", sql);
        Assert.Contains("ON CONFLICT (code) DO UPDATE", sql);
        Assert.Contains("JOIN (VALUES", sql);
        Assert.Contains("ON CONFLICT (plan_id) DO UPDATE", sql);
        Assert.Contains("ON CONFLICT (plan_id,capability_code) DO UPDATE", sql);
    }
}
