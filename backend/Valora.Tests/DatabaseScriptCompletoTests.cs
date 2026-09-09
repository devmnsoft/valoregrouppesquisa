using Valora.Tests.Support;
using Xunit;

namespace Valora.Tests;

[Trait("Category", "DatabaseContract")]
public sealed class DatabaseScriptCompletoTests {
    [Fact]
    public void ScriptCompletoContainsRequiredBootstrapObjects() {
        var sql = File.ReadAllText(RepositoryPaths.CanonicalDatabaseScript);
        Assert.Contains("CREATE SCHEMA IF NOT EXISTS valorapesquisa", sql);
        Assert.Contains("CREATE EXTENSION IF NOT EXISTS pgcrypto", sql);
        Assert.Contains("CREATE TABLE IF NOT EXISTS valorapesquisa.organizations", sql);
        Assert.Contains("CREATE TABLE IF NOT EXISTS valorapesquisa.survey_links", sql);
        Assert.Contains("'valora-official'", sql);
        Assert.Contains("ON CONFLICT", sql);
    }
}
