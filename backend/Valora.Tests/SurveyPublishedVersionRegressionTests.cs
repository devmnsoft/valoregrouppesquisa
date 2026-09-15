using Xunit;
using Valora.Tests.Support;

namespace Valora.Tests;

public sealed class SurveyPublishedVersionRegressionTests {
    [Fact]
    public void Public_collection_is_pinned_to_the_survey_version() {
        var validator = File.ReadAllText(RepositoryPaths.ApplicationFile("Services", "PublicSurveys", "PublicSurveyValidator.cs"));
        var forms = File.ReadAllText(RepositoryPaths.InfrastructureFile("Repositories", "FormRepository.cs"));
        var surveys = File.ReadAllText(RepositoryPaths.InfrastructureFile("Repositories", "SurveyRepository.cs"));

        Assert.Contains("survey.FormVersionId", validator, StringComparison.Ordinal);
        Assert.Contains("fv.id=@formVersionId AND fv.status='published'", forms, StringComparison.Ordinal);
        Assert.Contains("s.form_version_id AS FormVersionId", surveys, StringComparison.Ordinal);
        Assert.DoesNotContain("latest_published_version_id", validator, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Completion_rechecks_collection_under_a_database_lock() {
        var transaction = File.ReadAllText(RepositoryPaths.ApplicationFile("Services", "PublicSurveys", "PublicResponseTransactionService.cs"));
        var surveys = File.ReadAllText(RepositoryPaths.InfrastructureFile("Repositories", "SurveyRepository.cs"));

        Assert.Contains("LockEligibleForCompletionAsync", transaction, StringComparison.Ordinal);
        Assert.Contains("FOR UPDATE OF s", surveys, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("form_version_id", surveys, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Legacy_backfill_uses_only_the_originating_survey() {
        var sql = File.ReadAllText(RepositoryPaths.CanonicalDatabaseScript);
        Assert.Contains("SET form_version_id=s.form_version_id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SET form_version_id=f.latest_published_version_id", sql, StringComparison.OrdinalIgnoreCase);
    }
}
