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
    public void Processing_uses_the_immutable_diagnosis_methodology_snapshot()
    {
        Assert.Contains("survey_methodology_question_snapshots", Repository);
        Assert.DoesNotContain("LEFT JOIN LATERAL (SELECT x.* FROM valorapesquisa.question_", Repository);
        Assert.Contains("capture_survey_methodology_snapshot", Schema);
        Assert.Contains("methodology_snapshot_hash", Schema);
        Assert.Contains("selected_count<>1", Schema);
        Assert.Contains("Legacy diagnoses without a snapshot intentionally remain pending", Repository);
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

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Valora.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Valora.sln não foi localizado.");
    }
}
