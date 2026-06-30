using Xunit;

namespace Valora.Tests;

public sealed class DatabaseScriptCompletoTests
{
    [Fact]
    public void ScriptCompletoContainsRequiredBootstrapObjects()
    {
        var sql = File.ReadAllText(Path.Combine("..", "..", "..", "..", "scriptbd_completo.sql"));
        Assert.Contains("CREATE SCHEMA IF NOT EXISTS valorapesquisa", sql);
        Assert.Contains("CREATE EXTENSION IF NOT EXISTS pgcrypto", sql);
        Assert.Contains("CREATE TABLE IF NOT EXISTS valorapesquisa.organizations", sql);
        Assert.Contains("CREATE TABLE IF NOT EXISTS valorapesquisa.survey_links", sql);
        Assert.Contains("Diagnóstico gratuito Valora Insight", sql);
    }
}
