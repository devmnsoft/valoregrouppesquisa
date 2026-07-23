namespace Valora.Tests;

public sealed class PostgreSqlCanonicalScriptTests
{
    private static readonly string Root = LocateRepositoryRoot();

    [Fact]
    public void CanonicalScriptIsIdempotentAndNonDestructiveByInspection()
    {
        var sql = File.ReadAllText(Path.Combine(Root, "database", "postgresql", "banco_completo.sql"));
        Assert.Contains("CREATE SCHEMA IF NOT EXISTS valorapesquisa", sql);
        Assert.Contains("CREATE TABLE IF NOT EXISTS organizations", sql);
        Assert.Contains("CREATE UNIQUE INDEX IF NOT EXISTS ux_legal_entities_org_cnpj_active", sql);
        Assert.Contains("DROP TRIGGER IF EXISTS", sql);
        Assert.Contains("CREATE TRIGGER", sql);
        Assert.Contains("ON CONFLICT", sql);
        Assert.DoesNotContain("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP SCHEMA", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanonicalScriptSeedsOfficialPlansAndValoraSurvey()
    {
        var sql = File.ReadAllText(Path.Combine(Root, "database", "postgresql", "banco_completo.sql"));
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
        Assert.Contains("generate_series(1,5)", sql);
    }

    private static string LocateRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "package.json")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
