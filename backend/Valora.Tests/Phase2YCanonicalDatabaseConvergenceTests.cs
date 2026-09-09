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
        Assert.Matches(new Regex(@"INSERT INTO valorapesquisa\.forms\([^;]+SELECT id,'valora-official'", RegexOptions.Singleline), Sql);
        Assert.Contains("ALTER TABLE valorapesquisa.forms ALTER COLUMN organization_id SET NOT NULL", Sql);
    }

    [Fact]
    public void HistoricalAliasesAreWrittenTogetherBySeeds()
    {
        Assert.Matches(@"(?is)plan_capabilities\s*\(\s*plan_id\s*,\s*capability\s*,\s*capability_code\s*,\s*capability_key\s*,\s*enabled\s*,\s*is_enabled\s*\)", Sql);
        Assert.Matches(@"(?is)form_versions\s*\(\s*form_id\s*,\s*organization_id\s*,\s*version\s*,\s*version_number", Sql);
        Assert.Matches(@"(?is)dimensions\s*\(\s*form_version_id\s*,\s*code\s*,\s*name\s*,\s*position\s*,\s*display_order", Sql);
        Assert.Matches(@"(?is)questions\s*\([^)]*dimension_id\s*,\s*code\s*,[^)]*position\s*,\s*display_order", Sql);
    }
}
