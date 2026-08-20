using Valora.Tests.Support;

namespace Valora.Tests;

public sealed class IntelligenceProcessingMaterializationTests
{
    private static readonly string Repository = File.ReadAllText(Path.Combine(
        RepositoryPaths.RepositoryRoot, "backend", "Valora.Infrastructure", "Repositories", "IntelligenceProcessingJobRepository.cs"));

    [Fact]
    public void JobAndStageQueries_UseExplicitReusableProjections()
    {
        Assert.DoesNotContain("SELECT *", Repository, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JobProjection", Repository, StringComparison.Ordinal);
        Assert.Contains("StageProjection", Repository, StringComparison.Ordinal);
        Assert.Contains("organization_id AS \"OrganizationId\"", Repository, StringComparison.Ordinal);
        Assert.Contains("evidence_ids::text AS \"EvidenceJson\"", Repository, StringComparison.Ordinal);
    }

    [Fact]
    public void PersistenceRows_AreMutableAndPreserveNullableDatabaseColumns()
    {
        Assert.Contains("private sealed class IntelligenceProcessingJobRow", Repository, StringComparison.Ordinal);
        Assert.Contains("Guid? RunId", Repository, StringComparison.Ordinal);
        Assert.Contains("DateTime? LockedAt", Repository, StringComparison.Ordinal);
        Assert.Contains("string? MetadataJson", Repository, StringComparison.Ordinal);
        Assert.Contains("private sealed class StageRow", Repository, StringComparison.Ordinal);
        Assert.Contains("string? EvidenceJson", Repository, StringComparison.Ordinal);
        Assert.Contains("DeserializeEvidence", Repository, StringComparison.Ordinal);
    }

    [Fact]
    public void Worker_IsRegisteredExactlyOnce()
    {
        var program = File.ReadAllText(Path.Combine(RepositoryPaths.RepositoryRoot, "backend", "Valora.Api", "Program.cs"));
        Assert.Equal(1, Count(program, "AddHostedService<IntelligenceProcessingWorker>"));
    }

    private static int Count(string source, string value) =>
        source.Split(value, StringSplitOptions.None).Length - 1;
}
