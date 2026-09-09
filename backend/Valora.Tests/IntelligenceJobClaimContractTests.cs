using Valora.Tests.Support;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class IntelligenceJobClaimContractTests
{
    [Fact]
    public void RepositoryClaimsDueJobsAtomicallyAndUsesDeadLetterAfterRetries()
    {
        var source = File.ReadAllText(RepositoryPaths.InfrastructureFile("Repositories", "IntelligenceProcessingJobRepository.cs"));

        Assert.Contains("FOR UPDATE SKIP LOCKED", source, StringComparison.Ordinal);
        Assert.Contains("WITH candidates AS", source, StringComparison.Ordinal);
        Assert.Contains("UPDATE valorapesquisa.intelligence_processing_jobs AS job", source, StringComparison.Ordinal);
        Assert.Contains("RETURNING job.*", source, StringComparison.Ordinal);
        Assert.Contains("status='dead_letter'", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetPendingJobsAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LockJobAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkerProcessesOnlyJobsReturnedByTheAtomicClaim()
    {
        var source = File.ReadAllText(RepositoryPaths.ApiFile("IntelligenceProcessingWorker.cs"));

        Assert.Contains("ClaimPendingJobsAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LockJobAsync", source, StringComparison.Ordinal);
    }
}
