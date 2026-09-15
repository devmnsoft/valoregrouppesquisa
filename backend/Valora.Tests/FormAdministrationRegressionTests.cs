using Valora.Tests.Support;

namespace Valora.Tests;

public sealed class FormAdministrationRegressionTests {
    private static readonly string RepositorySource = Read("Valora.Infrastructure", "Repositories", "FormAdministrationRepository.cs");

    [Fact]
    public void GetQuery_UsesStableQuotedAliasesAndExplicitDatabaseTypes() {
        string[] requiredAliases =
        [
            "AS \"Id\"", "AS \"OrganizationId\"", "AS \"Name\"", "AS \"Description\"",
            "AS \"Category\"", "AS \"EstimatedMinutes\"", "AS \"Status\"",
            "AS \"CurrentDraftVersionId\"", "AS \"LatestPublishedVersionId\"",
            "AS \"Version\"", "AS \"DraftVersion\""
        ];

        Assert.All(requiredAliases, alias => Assert.Contains(alias, RepositorySource, StringComparison.Ordinal));
        Assert.Contains("COALESCE(f.version, 0)::bigint AS \"Version\"", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("fv.row_version::bigint AS \"DraftVersion\"", RepositorySource, StringComparison.Ordinal);
        Assert.DoesNotContain("SELECT *", RepositorySource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormRow_IsPropertyMaterializableAndPreservesNullableDatabaseValues() {
        Assert.Contains("private sealed class FormRow", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("public string? Description { get; init; }", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("public Guid? CurrentDraftVersionId { get; init; }", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("public Guid? LatestPublishedVersionId { get; init; }", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("public long? DraftVersion { get; init; }", RepositorySource, StringComparison.Ordinal);
        Assert.DoesNotContain("record FormRow(", RepositorySource, StringComparison.Ordinal);
    }

    [Fact]
    public void GetEndpoint_RejectsEmptyIdentifiersAndReturnsSanitizedCorrelatedProblems() {
        var controller = Read("Valora.Api", "Controllers", "FormsController.cs");

        Assert.Contains("if (formId == Guid.Empty)", controller, StringComparison.Ordinal);
        Assert.Contains("Formulário não encontrado", controller, StringComparison.Ordinal);
        Assert.Contains("[\"correlationId\"] = HttpContext.TraceIdentifier", controller, StringComparison.Ordinal);
        Assert.Contains("não existe ou não pertence à organização selecionada", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("return forms.GetAsync(Guid.Empty", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void OptionWrites_RequireACompatibleQuestionTypeInsideTheScopedSql() {
        Assert.Contains("q.type IN ('likert_1_5','single_choice','multiple_choice')", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("NOT EXISTS (", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("existing.question_id=q.id AND existing.deleted_at IS NULL", RepositorySource, StringComparison.Ordinal);
    }

    [Fact]
    public void LibraryPagination_IsTenantScopedDeterministicAndUsesTheSameFiltersForTotal() {
        Assert.Contains("Task<FormListResponse> ListAsync", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("LIMIT @pageSize OFFSET @offset", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("ORDER BY COALESCE(f.updated_at, f.created_at) DESC, f.id", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("COUNT(*)::bigint AS \"Total\"", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("checked(((long)query.Page - 1L) * query.PageSize)", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("su.organization_id=@organizationId", RepositorySource, StringComparison.Ordinal);
    }

    [Fact]
    public void UsageAndArchive_AreBasedOnRealSurveyLinksAndPreserveHistory() {
        Assert.Contains("AS \"InCurrentUse\"", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("AS \"HasHistoricalUse\"", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("AS \"HasResponses\"", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("s.status IN ('active','published','open')", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("form.archived", RepositorySource, StringComparison.Ordinal);
        Assert.DoesNotContain("DELETE FROM valorapesquisa.forms", RepositorySource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Publication_UsesOnlyEligibleNonArchivedRespondableStructure() {
        Assert.Contains("s.deleted_at IS NULL AND q.deleted_at IS NULL", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("q.type NOT IN ('heading','description','separator')", RepositorySource, StringComparison.Ordinal);
    }

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepositoryPaths.RepositoryRoot, "backend", .. parts]));
}
