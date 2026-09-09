using Valora.Tests.Support;
using Xunit;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class AdminRepositoryMigrationTests {
    [Fact]
    public void WebAdminModulesControllerDoesNotExposeRepositoryRequired501ForMainAdminEndpoints() {
        var controller = File.ReadAllText(RepositoryPaths.ApiFile("Controllers", "WebAdminModulesController.cs"));
        Assert.DoesNotContain("WEB_ADMIN_REAL_REPOSITORY_REQUIRED", controller);
        Assert.DoesNotContain("StatusCode(501", controller);
        Assert.Contains("IOrganizationRepository", controller);
        Assert.Contains("ISurveyRepository", controller);
        Assert.Contains("IResponseRepository", controller);
        Assert.Contains("IAuditRepository", controller);
        Assert.Contains("ICurrentRequestContext", controller);
    }

    [Fact]
    public void CompleteDatabaseScriptKeepsUsersCompatibleWithRepositoriesAndRoles() {
        var sql = File.ReadAllText(RepositoryPaths.CanonicalDatabaseScript);
        Assert.Contains("CREATE TABLE IF NOT EXISTS valorapesquisa.user_roles", sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS role_id uuid", sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS phone text", sql);
        Assert.Contains("organization_id uuid NOT NULL UNIQUE REFERENCES valorapesquisa.organizations(id), settings jsonb", sql);
    }

    [Fact]
    public void CanonicalPostgresScriptIncludesColumnsUsedByAdminRepositories() {
        var sql = File.ReadAllText(RepositoryPaths.CanonicalDatabaseScript);
        Assert.Contains("role_id", sql);
        Assert.Contains("organization_settings", sql);
        Assert.Contains("revoked_at", sql);
        Assert.Contains("plan_id", sql);
    }
    [Fact]
    public void CompleteDatabaseScriptContainsOrganizationColumnsRequiredByAdminRepository() {
        var sql = File.ReadAllText(RepositoryPaths.CanonicalDatabaseScript);
        foreach (var column in new[] { "public_name", "email", "phone", "default_language_code", "time_zone", "onboarding_status", "legal_name", "cnpj", "minimum_aggregation_size" }) {
            Assert.Contains(column, sql);
        }
    }

    [Fact]
    public void CompleteDatabaseScriptUsesOfficialCodeBasedPlanSchema() {
        var sql = File.ReadAllText(RepositoryPaths.CanonicalDatabaseScript);
        Assert.Contains("code text NOT NULL UNIQUE", sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS monthly_price numeric", sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS annual_price numeric", sql);
        Assert.Contains("plan_limits(plan_id,limit_key,limit_value,period)", sql);
        Assert.Contains("plan_features(plan_id,feature_key,enabled)", sql);
        Assert.Contains("capability_code", sql);
        Assert.Contains("enabled", sql);
        Assert.Matches(@"(?i)plan_code\s+(?:text|varchar\(60\))\s+not null default 'free'", sql);
        Assert.DoesNotContain("price_label", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("organizations(name,public_name,slug,status,plan_id)", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdminRepositoriesDoNotSelectSensitiveHashColumnsForListEndpoints() {
        var repositoriesDir = RepositoryPaths.InfrastructureFile("Repositories");
        var userRepository = File.ReadAllText(Path.Combine(repositoriesDir, "UserRepository.cs"));
        var surveyRepository = File.ReadAllText(Path.Combine(repositoriesDir, "SurveyRepository.cs"));
        var responseRepository = File.ReadAllText(Path.Combine(repositoriesDir, "ResponseRepository.cs"));
        Assert.Contains("private const string UserProjection", userRepository);
        Assert.Contains("ListByOrganizationAsync", userRepository);
        Assert.Contains("ListAdminAsync", surveyRepository);
        Assert.Contains("ListAdminAsync", responseRepository);
        Assert.DoesNotContain("SELECT u.id,u.password_hash", userRepository, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SELECT id,organization_id,survey_id,token_hash", surveyRepository, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SELECT r.id,r.result_token_hash", responseRepository, StringComparison.OrdinalIgnoreCase);
    }

}
