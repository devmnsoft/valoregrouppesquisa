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
    [Fact]
    public void CompleteDatabaseScriptContainsOrganizationColumnsRequiredByAdminRepository()
    {
        var sql = File.ReadAllText(Path.Combine("..", "..", "..", "..", "scriptbd_completo.sql"));
        foreach (var column in new[] { "document text", "email citext", "phone text", "settings_json jsonb", "brand_json jsonb", "deleted_at timestamptz", "deleted_by uuid" })
        {
            Assert.Contains(column, sql);
        }
    }

    [Fact]
    public void CompleteDatabaseScriptUsesTextPlanLimitShapeExpectedByPlanRepository()
    {
        var sql = File.ReadAllText(Path.Combine("..", "..", "..", "..", "scriptbd_completo.sql"));
        Assert.Contains("id text primary key", sql);
        Assert.Contains("visible_on_public_pricing boolean", sql);
        Assert.Contains("plan_id text not null, limit_key text not null, limit_value int not null", sql);
        Assert.Contains("plan_id text not null, capability_key text not null, capability_level text not null", sql);
        Assert.Contains("organization_id uuid not null, plan_id text not null", sql);
        Assert.DoesNotContain("plan_id uuid not null, active_surveys", sql);
        Assert.DoesNotContain("capability citext not null, enabled boolean", sql);
    }

    [Fact]
    public void AdminRepositoriesDoNotSelectSensitiveHashColumnsForListEndpoints()
    {
        var repositoriesDir = Path.Combine("..", "..", "..", "..", "backend", "Valora.Infrastructure", "Repositories");
        var userRepository = File.ReadAllText(Path.Combine(repositoriesDir, "UserRepository.cs"));
        var surveyRepository = File.ReadAllText(Path.Combine(repositoriesDir, "SurveyRepository.cs"));
        var responseRepository = File.ReadAllText(Path.Combine(repositoriesDir, "ResponseRepository.cs"));
        Assert.Contains("SELECT u.id,u.organization_id,u.name,u.email,COALESCE(u.role,r.code) AS role,u.status,u.phone,u.last_login_at,u.created_at", userRepository);
        Assert.Contains("SELECT id,organization_id,survey_id,public_url,status,starts_at,expires_at,created_at,updated_at", surveyRepository);
        Assert.Contains("SELECT r.id,r.organization_id,r.survey_id,r.form_id,r.participant_name,r.participant_email,r.status", responseRepository);
        Assert.DoesNotContain("SELECT u.id,u.password_hash", userRepository, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SELECT id,organization_id,survey_id,token_hash", surveyRepository, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SELECT r.id,r.result_token_hash", responseRepository, StringComparison.OrdinalIgnoreCase);
    }

}
