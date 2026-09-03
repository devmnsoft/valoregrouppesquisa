using Valora.Tests.Support;

namespace Valora.Tests;

public sealed class FormAdministrationRegressionTests
{
    private static readonly string RepositorySource = Read("Valora.Infrastructure", "Repositories", "FormAdministrationRepository.cs");

    [Fact]
    public void GetQuery_UsesStableQuotedAliasesAndExplicitDatabaseTypes()
    {
        string[] requiredAliases =
        [
            "AS \"Id\"", "AS \"OrganizationId\"", "AS \"Name\"", "AS \"Description\"",
            "AS \"Category\"", "AS \"EstimatedMinutes\"", "AS \"Status\"",
            "AS \"CurrentDraftVersionId\"", "AS \"LatestPublishedVersionId\"",
            "AS \"Version\"", "AS \"DraftVersion\""
        ];

        Assert.All(requiredAliases, alias => Assert.Contains(alias, RepositorySource, StringComparison.Ordinal));
        Assert.Contains("COALESCE(f.version, 0)::bigint AS \"Version\"", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("fv.version::int AS \"DraftVersion\"", RepositorySource, StringComparison.Ordinal);
        Assert.DoesNotContain("SELECT *", RepositorySource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormRow_IsPropertyMaterializableAndPreservesNullableDatabaseValues()
    {
        Assert.Contains("private sealed class FormRow", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("public string? Description { get; init; }", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("public Guid? CurrentDraftVersionId { get; init; }", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("public Guid? LatestPublishedVersionId { get; init; }", RepositorySource, StringComparison.Ordinal);
        Assert.Contains("public int? DraftVersion { get; init; }", RepositorySource, StringComparison.Ordinal);
        Assert.DoesNotContain("record FormRow(", RepositorySource, StringComparison.Ordinal);
    }

    [Fact]
    public void GetEndpoint_RejectsEmptyIdentifiersAndReturnsSanitizedCorrelatedProblems()
    {
        var controller = Read("Valora.Api", "Controllers", "FormsController.cs");

        Assert.Contains("if (formId == Guid.Empty)", controller, StringComparison.Ordinal);
        Assert.Contains("Formulário não encontrado", controller, StringComparison.Ordinal);
        Assert.Contains("CorrelationId={CorrelationId}", controller, StringComparison.Ordinal);
        Assert.Contains("Verifique se a organização está selecionada e tente novamente.", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("return forms.GetAsync(Guid.Empty", controller, StringComparison.Ordinal);
    }

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepositoryPaths.RepositoryRoot, "backend", .. parts]));
}
