using Xunit;

namespace Valora.Tests;

public sealed class AdminRepositoryMigrationTests
{
    [Fact]
    public void WebAdminModulesControllerDoesNotExposeRepositoryRequired501ForMainAdminEndpoints()
    {
        var controller = File.ReadAllText(Path.Combine("..", "..", "..", "..", "backend", "Valora.Api", "Controllers", "WebAdminModulesController.cs"));
        Assert.DoesNotContain("WEB_ADMIN_REAL_REPOSITORY_REQUIRED", controller);
        Assert.DoesNotContain("StatusCode(501", controller);
        Assert.Contains("IOrganizationRepository", controller);
        Assert.Contains("IUserRepository", controller);
        Assert.Contains("IFormRepository", controller);
        Assert.Contains("ISurveyRepository", controller);
        Assert.Contains("IResponseRepository", controller);
        Assert.Contains("IAuditRepository", controller);
    }

    [Fact]
    public void CompleteDatabaseScriptKeepsUsersCompatibleWithRepositoriesAndRoles()
    {
        var sql = File.ReadAllText(Path.Combine("..", "..", "..", "..", "scriptbd_completo.sql"));
        Assert.Contains("role text not null default 'empresa_admin'", sql);
        Assert.Contains("role_id uuid null", sql);
        Assert.Contains("phone text null", sql);
        Assert.Contains("organization_id uuid not null unique, settings jsonb", sql);
    }

    [Fact]
    public void ModularPostgresScriptsIncludeColumnsUsedByAdminRepositories()
    {
        var core = File.ReadAllText(Path.Combine("..", "..", "..", "..", "database", "postgresql", "002_core_tables.sql"));
        var surveys = File.ReadAllText(Path.Combine("..", "..", "..", "..", "database", "postgresql", "004_survey_tables.sql"));
        Assert.Contains("role_id", core);
        Assert.Contains("organization_settings", core);
        Assert.Contains("revoked_at", surveys);
        Assert.Contains("plan_id", surveys);
    }
}
