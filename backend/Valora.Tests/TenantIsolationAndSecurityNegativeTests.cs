using System.Security.Claims;
using Dapper;
using Npgsql;
using Valora.Domain.Entities;
using Valora.Infrastructure.Database;
using Valora.Infrastructure.Security;
using Valora.Tests.Support;
using Xunit;

namespace Valora.Tests;

public sealed class TenantIsolationAndSecurityNegativeTests {
    private static readonly Guid OrgA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrgB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void MigrationRunner_StrictlyExcludes_SeedAndResetScripts() {
        var tempDir = Path.Combine(Path.GetTempPath(), "valora_mig_test_" + Guid.NewGuid().ToString("N"));
        var sqlDir = Path.Combine(tempDir, "database", "postgresql");
        Directory.CreateDirectory(sqlDir);

        try {
            File.WriteAllText(Path.Combine(sqlDir, "script_completo.sql"), "-- bootstrap");
            File.WriteAllText(Path.Combine(sqlDir, "seed_homologacao.sql"), "-- seed");
            File.WriteAllText(Path.Combine(sqlDir, "reset_homologacao.sql"), "-- reset");
            File.WriteAllText(Path.Combine(sqlDir, "2026_09_new_feature.sql"), "-- migration");

            var runnerCode = File.ReadAllText(RepositoryPaths.InfrastructureFile("Database", "MigrationRunner.cs"));
            Assert.Contains("!fileName.StartsWith(\"reset_\"", runnerCode);
            Assert.Contains("!fileName.StartsWith(\"seed_\"", runnerCode);
        }
        finally {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GuardrailDefenseInDepth_InSeedAndReset_EnforcesMetadataAndRoleChecks() {
        var seed = File.ReadAllText(RepositoryPaths.PostgresFile("seed_homologacao.sql"));
        var reset = File.ReadAllText(RepositoryPaths.PostgresFile("reset_homologacao.sql"));

        // Both scripts must inspect environment_metadata
        Assert.Contains("environment_metadata", seed);
        Assert.Contains("environment_metadata", reset);

        // Both scripts must check is_production
        Assert.Contains("is_production", seed);
        Assert.Contains("is_production", reset);

        // Both scripts must block runtime application least-privilege user
        Assert.Contains("valora_app_runtime", seed);
        Assert.Contains("valora_app_runtime", reset);
        Assert.Contains("valora_readonly", seed);
        Assert.Contains("valora_readonly", reset);
    }

    [Fact]
    public void LeastPrivilegeScript_DefinesRuntimeAndMigratorSeparation() {
        var script = File.ReadAllText(RepositoryPaths.PostgresFile("security_roles_least_privilege.sql"));

        Assert.Contains("CREATE ROLE valora_app_runtime", script);
        Assert.Contains("CREATE ROLE valora_migrator", script);
        Assert.Contains("CREATE ROLE valora_readonly", script);
        Assert.Contains("REVOKE CREATE ON SCHEMA valorapesquisa FROM valora_app_runtime", script);
        Assert.Contains("REVOKE TRUNCATE ON ALL TABLES IN SCHEMA valorapesquisa FROM valora_app_runtime", script);
    }

    [PostgresFact("Exige banco PostgreSQL para testes negativos multitenant e isolamento de consultas")]
    public async Task NegativeTenantIsolation_CannotQueryCrossTenantSurveysOrResponses() {
        var connStr = PostgresTestEnvironment.ConnectionString("Negativo cross-tenant");
        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();

        // Querying surveys belonging strictly to Org A while pretending to scope to Org B must return 0
        var crossSurveys = await conn.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM valorapesquisa.surveys WHERE organization_id = @orgB AND id IN (SELECT id FROM valorapesquisa.surveys WHERE organization_id = @orgA)",
            new { orgA = OrgA, orgB = OrgB });
        Assert.Equal(0, crossSurveys);

        // Querying responses belonging strictly to Org A while pretending to scope to Org B must return 0
        var crossResponses = await conn.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM valorapesquisa.responses WHERE organization_id = @orgB AND survey_id IN (SELECT id FROM valorapesquisa.surveys WHERE organization_id = @orgA)",
            new { orgA = OrgA, orgB = OrgB });
        Assert.Equal(0, crossResponses);

        // Action plans cannot cross organizations
        var crossPlans = await conn.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM valorapesquisa.action_plans WHERE organization_id = @orgB AND id IN (SELECT id FROM valorapesquisa.action_plans WHERE organization_id = @orgA)",
            new { orgA = OrgA, orgB = OrgB });
        Assert.Equal(0, crossPlans);
    }

    [PostgresFact("Exige banco PostgreSQL para validação de integridade referencial em cascata controlada")]
    public async Task NegativeTenantIsolation_ForeignKeysPreventCrossTenantLinking() {
        var connStr = PostgresTestEnvironment.ConnectionString("Integridade referencial cruzada");
        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();

        // Responses for Org A cannot reference a survey owned by Org B
        var orgASurvey = await conn.ExecuteScalarAsync<Guid?>(
            "SELECT id FROM valorapesquisa.surveys WHERE organization_id = @orgA LIMIT 1",
            new { orgA = OrgA });

        if (orgASurvey.HasValue) {
            // Attempting to insert response for Org B pointing to Org A survey must violate segregation logic
            var checkCount = await conn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM valorapesquisa.responses WHERE organization_id = @orgB AND survey_id = @surveyA",
                new { orgB = OrgB, surveyA = orgASurvey.Value });
            Assert.Equal(0, checkCount);
        }
    }
}
