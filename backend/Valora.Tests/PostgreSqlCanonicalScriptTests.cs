namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class PostgreSqlCanonicalScriptTests
{
    [Fact]
    public void CanonicalScriptIsIdempotentAndNonDestructiveByInspection()
    {
        var sql = File.ReadAllText(Support.RepositoryPaths.CanonicalDatabaseScript);
        Assert.Contains("CREATE SCHEMA IF NOT EXISTS valorapesquisa", sql);
        Assert.Contains("CREATE TABLE IF NOT EXISTS valorapesquisa.organizations", sql);
        Assert.Contains("CREATE UNIQUE INDEX IF NOT EXISTS ux_legal_entities_cnpj_active", sql);
        Assert.Contains("WHERE deleted_at IS NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE UNIQUE INDEX IF NOT EXISTS ux_legal_entities_org_cnpj_active", sql);
        Assert.Contains("pg_get_triggerdef", sql);
        Assert.Contains("CREATE TRIGGER", sql);
        Assert.Contains("ON CONFLICT", sql);
        Assert.DoesNotContain("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP SCHEMA", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanonicalScriptSafelyReplacesOrganizationScopedCnpjIndex()
    {
        var sql = File.ReadAllText(Support.RepositoryPaths.CanonicalDatabaseScript);

        Assert.Contains("DROP INDEX IF EXISTS ux_legal_entities_org_cnpj_active", sql);
        Assert.Contains("CREATE UNIQUE INDEX IF NOT EXISTS ux_legal_entities_cnpj_active", sql);
        Assert.Contains("WHERE deleted_at IS NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP SCHEMA", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanonicalScriptSeedsOfficialPlansAndValoraSurvey()
    {
        var sql = File.ReadAllText(Support.RepositoryPaths.CanonicalDatabaseScript);
        foreach (var plan in new[] { "free", "professional", "corporate", "enterprise" })
        {
            Assert.Contains($"'{plan}'", sql);
        }
        Assert.Contains("'essential'", sql);
        Assert.Contains("'growth'", sql);
        Assert.Contains("Cultura e Propósito", sql);
        Assert.Contains("Gestão e Governança", sql);
        Assert.Contains("Liderança", sql);
        Assert.Contains("Pessoas e Talentos", sql);
        Assert.Contains("Resultados e Crescimento", sql);
        Assert.Contains("growth-q5", sql);
        Assert.Equal(25, System.Text.RegularExpressions.Regex.Matches(sql, @"'(?:(?:culture|governance|leadership|people|growth)-q[1-5])'").Count);
    }

}
