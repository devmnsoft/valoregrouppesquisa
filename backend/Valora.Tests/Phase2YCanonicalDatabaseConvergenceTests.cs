using System.Text.RegularExpressions;
using Valora.Tests.Support;
using Xunit;

namespace Valora.Tests;

[Trait("Category", "DatabaseContract")]
public sealed class Phase2YCanonicalDatabaseConvergenceTests
{
    private static readonly string Sql = File.ReadAllText(RepositoryPaths.CanonicalDatabaseScript);

    [Theory]
    [InlineData("permissions", "module_code")]
    [InlineData("permissions", "functional_group")]
    [InlineData("permissions", "risk_level")]
    [InlineData("plan_capabilities", "capability")]
    [InlineData("plan_capabilities", "capability_code")]
    [InlineData("plan_capabilities", "capability_key")]
    [InlineData("plan_capabilities", "is_enabled")]
    [InlineData("forms", "organization_id")]
    [InlineData("form_versions", "version_number")]
    [InlineData("form_versions", "maximum_score")]
    [InlineData("dimensions", "position")]
    [InlineData("questions", "position")]
    [InlineData("question_options", "position")]
    public void CanonicalScriptConvergesHistoricalColumn(string table, string column)
        => Assert.Contains($"ALTER TABLE valorapesquisa.{table} ADD COLUMN IF NOT EXISTS {column}", Sql, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void OfficialFormIsOwnedByStablePlatformOrganization()
    {
        Assert.Contains("VALUES('Valora Group','valora-platform','active')", Sql);
        Assert.Matches(new Regex(@"INSERT INTO valorapesquisa\.forms\(organization_id,title,name,code,slug,form_key,status,questions_count,estimated_minutes,version,deleted_at\).*'valora-official'", RegexOptions.Singleline), Sql);
        Assert.Contains("ALTER TABLE valorapesquisa.forms ALTER COLUMN organization_id SET NOT NULL", Sql);
    }

    [Fact]
    public void HistoricalAliasesAreWrittenTogetherBySeeds()
    {
        Assert.Contains("plan_capabilities(plan_id,capability,capability_code,capability_key,enabled,is_enabled)", Sql);
        Assert.Contains("form_versions(form_id,organization_id,version,version_number,language,is_immutable,maximum_score,max_score,status,published_at)", Sql);
        Assert.Contains("dimensions(form_version_id,code,name,position,display_order,max_score)", Sql);
        Assert.Contains("questions(dimension_id,code,text,position,display_order", Sql);
    }
}
