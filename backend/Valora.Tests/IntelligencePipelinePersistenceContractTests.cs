namespace Valora.Tests;

public sealed class IntelligencePipelinePersistenceContractTests
{
    private static readonly string Repository = File.ReadAllText(Path.Combine(
        FindRoot(), "Valora.Infrastructure", "Repositories", "IntelligencePipelineRepository.cs"));
    private static readonly string Schema = File.ReadAllText(Path.Combine(
        FindRoot(), "database", "postgresql", "script_completo.sql"));

    [Fact]
    public void Retry_updates_the_same_operation_without_deleting_previous_results()
    {
        Assert.DoesNotContain("DELETE FROM valorapesquisa.{outputTable}", Repository);
        Assert.DoesNotContain("DELETE FROM valorapesquisa.{table}", Repository);
        Assert.Contains("ON CONFLICT (organization_id,run_id,code)", Repository);
        Assert.Contains("'metric_values','index_values','inference_runs','inference_results','insight_runs','insights'", Schema);
        Assert.Contains("'ux_'||table_name||'_operation_code'", Schema);
    }

    [Fact]
    public void Stage_persistence_is_atomic_and_preserves_published_projection()
    {
        Assert.Contains("BeginTransaction()", Repository);
        Assert.Contains("transaction.Commit()", Repository);
        Assert.Contains("status='published'", Repository);
        Assert.Contains("THEN valorapesquisa.{table}.data", Repository);
    }

    [Fact]
    public void Mapping_precedence_and_input_identity_are_explicit_and_deterministic()
    {
        Assert.Equal(3, Count("CASE WHEN x.organization_id=r.organization_id THEN 0 ELSE 1 END"));
        Assert.DoesNotContain("id,updated_at,normalized_value", Repository);
        Assert.Contains("ORDER BY concept_code,metric_code,index_code,normalized_value", Repository);
    }

    [Fact]
    public void Concurrent_events_are_guarded_by_database_uniqueness()
    {
        Assert.Contains("ux_notifications_intelligence_event", Schema);
        Assert.Contains("ux_journey_events_pipeline_event", Schema);
        Assert.Contains("ux_governance_events_pipeline_event", Schema);
        Assert.Contains("ON CONFLICT DO NOTHING", Repository);
    }

    private static int Count(string value)
    {
        var count = 0;
        for (var offset = 0; (offset = Repository.IndexOf(value, offset, StringComparison.Ordinal)) >= 0; offset += value.Length)
            count++;
        return count;
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Valora.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Valora.sln não foi localizado.");
    }
}
