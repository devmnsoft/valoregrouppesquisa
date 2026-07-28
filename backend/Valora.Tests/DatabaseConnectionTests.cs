using Xunit;

namespace Valora.Tests;

[Trait("Category", "Integration")]
public sealed class DatabaseConnectionTests
{
    [Fact]
    public void ConnectionStringUsesPostgresPort5434()
    {
        var json = File.ReadAllText(Support.RepositoryPaths.ApiFile("appsettings.json"));
        Assert.Contains("Port=5434", json);
    }
}
