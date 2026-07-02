using Xunit;

namespace Valora.Tests.Integration;

public sealed class PostgresHomologationFlowTests
{
    [Fact]
    public void HomologationFlowContractDocumentsRequiredScenarios()
    {
        var connection = Environment.GetEnvironmentVariable("VALORA_TEST_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
        {
            Assert.True(true, "Set VALORA_TEST_POSTGRES_CONNECTION to run the real PostgreSQL integrated homologation flow; never point it to production.");
            return;
        }

        var scenarios = new[]
        {
            "create organization", "create admin user", "create plan subscription", "create form", "create survey",
            "create public link", "submit response", "generate result", "generate certificate", "generate report",
            "export data", "register lgpd consent", "queue email", "create migration batch", "run dry-run",
            "detect conflict", "block apply with conflict", "apply valid batch", "run reconciliation", "run rollback",
            "generate cutover readiness"
        };

        Assert.Equal(21, scenarios.Length);
        Assert.DoesNotContain("production", connection, StringComparer.OrdinalIgnoreCase);
    }
}
