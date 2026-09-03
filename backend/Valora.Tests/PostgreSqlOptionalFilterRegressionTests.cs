using Valora.Tests.Support;

namespace Valora.Tests;

public sealed class PostgreSqlOptionalFilterRegressionTests
{
    [Theory]
    [InlineData("IntelligenceProcessingJobRepository.cs")]
    [InlineData("JourneyRepository.cs")]
    [InlineData("AssistedOperationsRepository.cs")]
    [InlineData("UserAdministrationRepository.cs")]
    public void OptionalFilters_HaveExplicitPostgreSqlTypes(string fileName)
    {
        var source = ReadRepository(fileName);

        Assert.DoesNotContain("@Search IS NULL", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@Status IS NULL", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@From IS NULL", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@OrganizationId IS NULL", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JourneyProjection_UsesQuotedPropertyAliases()
    {
        var source = ReadRepository("JourneyRepository.cs");

        Assert.Contains("AS \"EventType\"", source, StringComparison.Ordinal);
        Assert.Contains("AS \"OccurredAt\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SELECT *", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IntegerOperationsDashboardCounts_AreExplicitlyCast()
    {
        var source = ReadRepository("AssistedOperationsRepository.cs");

        Assert.Contains("count(*)::int", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadRepository(string fileName) => File.ReadAllText(Path.Combine(
        RepositoryPaths.RepositoryRoot, "backend", "Valora.Infrastructure", "Repositories", fileName));
}
