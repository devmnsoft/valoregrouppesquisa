using System.Data;
using Dapper;
using Npgsql;
using Valora.Application.Contracts;
using Valora.Application.Evolution;
using Valora.Application.ActionCenter;
using Valora.Infrastructure.Database;
using Valora.Infrastructure.Repositories;
using Valora.Tests.Support;

namespace Valora.Tests;

public sealed class EvolutionPersistencePostgresTests {
    private sealed class Connections(string connection) : IDbConnectionFactory {
        public IDbConnection Create() => new NpgsqlConnection(connection);
    }

    [PostgresFact("persistência de medições não executada.")]
    public async Task BaselinePreservesHistoricalReadingAndLegacyResultsRemainExplicit() {
        var factory = new Connections(PostgresTestEnvironment.ConnectionString("persistência de medições"));
        using var db = factory.Create();
        var o = Guid.NewGuid(); var survey = Guid.NewGuid();
        await db.ExecuteAsync("INSERT INTO valorapesquisa.organizations(id,name,slug) VALUES(@o,'Evolution test',@slug)", new { o, slug = o.ToString("N") });
        await db.ExecuteAsync("INSERT INTO valorapesquisa.surveys(id,organization_id,form_version_id,name,title,status) SELECT @survey,@o,id,'Evolution','Evolution','draft' FROM valorapesquisa.form_versions ORDER BY created_at LIMIT 1", new { survey, o });
        var repository = new EvolutionRepository(factory, new DbTransactionFactory(factory));
        var basis = new EvolutionComparisonBasis("professional@3", "people", "1-5", "weighted", "cohort-a", true);
        var measurement = new PreservedEvolutionMeasurement(Guid.NewGuid(), survey, "Original", 0, DateTime.UtcNow, 4, basis, "{\"original\":true}");
        var request = new OpenEvolutionCycleRequest(survey, null, null, "Historical cycle", "Preserve baseline", 0, null, null, DateTime.UtcNow, null, "Original evidence", BaselineSource: "organizational_measurement", SourceSurveyId: survey, Measurement: measurement);
        try {
            var id = await repository.Open(o, Guid.NewGuid(), request, default);
            await db.ExecuteAsync("UPDATE valorapesquisa.surveys SET title='Changed title' WHERE id=@survey", new { survey });
            var restored = await repository.Baseline(o, id, default);
            Assert.Equal(measurement, restored);
            Assert.Null(await repository.Baseline(Guid.NewGuid(), id, default));
            Assert.Null((await repository.Get(o, id, default))!.ResultId);
            Assert.Equal(0, (await repository.Get(o, id, default))!.BaselineScore);
            var legacy = Guid.NewGuid(); var result = Guid.NewGuid();
            await db.ExecuteAsync("INSERT INTO valorapesquisa.evolution_cycles(id,organization_id,result_id,title,metadata_json) VALUES(@legacy,@o,@result,'Legacy','{\"baselineSource\":\"organizational_measurement\"}')", new { legacy, o, result });
            Assert.Equal("legacy_review_required", (await repository.Get(o, legacy, default))!.BaselineSource);
            Assert.Null(await repository.Baseline(o, legacy, default));
            await db.ExecuteAsync("UPDATE valorapesquisa.evolution_cycles SET baseline_score=37.5,current_score=42 WHERE id=@legacy", new { legacy });
            var reviewCommand = Guid.NewGuid().ToString("N");
            var reviewer = Guid.NewGuid();
            var review = await repository.ReviewReference(o, reviewer, legacy, 1, "Arquivo histórico sem versão do processamento", "Solicitar a leitura original ao responsável", reviewCommand, default);
            Assert.Equal(review, await repository.ReviewReference(o, reviewer, legacy, 1, "Arquivo histórico sem versão do processamento", "Solicitar a leitura original ao responsável", reviewCommand, default));
            Assert.Equal(2, await repository.Version(o, legacy, default));
            Assert.Equal(result, (await repository.Get(o, legacy, default))!.ResultId);
            Assert.Equal(37.5m, (await repository.Get(o, legacy, default))!.BaselineScore);
            Assert.Equal(42m, (await repository.Get(o, legacy, default))!.CurrentScore);
            Assert.Equal("legacy_review_required", (await repository.Get(o, legacy, default))!.BaselineSource);
            Assert.Null(await repository.Baseline(o, legacy, default));
            var reviewSnapshot = Assert.Single(await repository.Snapshots(o, legacy, default));
            Assert.Equal("reference_review", reviewSnapshot.SnapshotType);
            Assert.Null(reviewSnapshot.Delta);
            Assert.Contains(reviewer.ToString(), reviewSnapshot.MetadataJson);
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.ReviewReference(o, reviewer, legacy, 1, "Different evidence", "Different next step", reviewCommand, default));
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.ReviewReference(o, reviewer, legacy, 1, "Evidence", "Next step", Guid.NewGuid().ToString("N"), default));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.ReviewReference(Guid.NewGuid(), reviewer, legacy, 2, "Evidence", "Next step", Guid.NewGuid().ToString("N"), default));
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.ReviewReference(o, reviewer, id, 1, "Evidence", "Next step", Guid.NewGuid().ToString("N"), default));
            await db.ExecuteAsync("UPDATE valorapesquisa.surveys SET status='active' WHERE id=@survey", new { survey });
            var response = Guid.NewGuid(); var resultScore = Guid.NewGuid();
            await db.ExecuteAsync("INSERT INTO valorapesquisa.responses(id,organization_id,survey_id,participant_email) VALUES(@response,@o,@survey,'sample@valora.test'); INSERT INTO valorapesquisa.result_scores(id,response_id,total_score,max_score) VALUES(@resultScore,@response,40,100); INSERT INTO valorapesquisa.dimension_scores(result_score_id,dimension_id,score,max_score) SELECT @resultScore,id,40,100 FROM valorapesquisa.dimensions WHERE code='people'", new { response, o, survey, resultScore });
            var intelligence = new OrganizationalIntelligenceRepository(factory, new DbTransactionFactory(factory));
            var reading = await intelligence.GetEvolutionSurveyAsync(o, survey, default);
            Assert.Equal(40, reading!.MaturityIndex);
            Assert.Contains(resultScore.ToString(), reading.ProcessingSourceJson);
            Assert.Null(await intelligence.GetEvolutionSurveyAsync(Guid.NewGuid(), survey, default));
            var liveMeasurement = EvolutionMeasurementBasis.Capture(reading);
            var liveCycle = await repository.Open(o, Guid.NewGuid(), request with { Measurement = liveMeasurement, BaselineScore = 40 }, default);
            await db.ExecuteAsync("UPDATE valorapesquisa.dimension_scores SET score=50 WHERE result_score_id=@resultScore; UPDATE valorapesquisa.result_scores SET updated_at=now() WHERE id=@resultScore", new { resultScore });
            Assert.Equal(50, (await intelligence.GetEvolutionSurveyAsync(o, survey, default))!.MaturityIndex);
            Assert.Equal(40, (await repository.Baseline(o, liveCycle, default))!.Score);
            Assert.Equal(liveMeasurement.SourceJson, (await repository.Baseline(o, liveCycle, default))!.SourceJson);
            var additionalResponse = Guid.NewGuid(); var additionalScore = Guid.NewGuid();
            await db.ExecuteAsync("INSERT INTO valorapesquisa.responses(id,organization_id,survey_id,participant_email) VALUES(@additionalResponse,@o,@survey,'new-sample@valora.test'); INSERT INTO valorapesquisa.result_scores(id,response_id,total_score,max_score) VALUES(@additionalScore,@additionalResponse,100,100); INSERT INTO valorapesquisa.dimension_scores(result_score_id,dimension_id,score,max_score) SELECT @additionalScore,id,100,100 FROM valorapesquisa.dimensions WHERE code='people'", new { additionalResponse, additionalScore, o, survey });
            Assert.Equal(75, (await intelligence.GetEvolutionSurveyAsync(o, survey, default))!.MaturityIndex);
            Assert.Equal(40, (await repository.Baseline(o, liveCycle, default))!.Score);
            Assert.Equal(1, (await repository.Baseline(o, liveCycle, default))!.ResponseCount);
            await db.ExecuteAsync("UPDATE valorapesquisa.surveys SET status='closed' WHERE id=@survey", new { survey });
            Assert.Equal(75, (await intelligence.GetEvolutionSurveyAsync(o, survey, default))!.MaturityIndex);
            await db.ExecuteAsync("INSERT INTO valorapesquisa.organization_modules(organization_id,module_code,enabled) VALUES(@o,'organizational_intelligence',true) ON CONFLICT DO NOTHING", new { o });
            var secondSurvey = Guid.NewGuid(); var firstPlan = Guid.NewGuid(); var secondPlan = Guid.NewGuid();
            await db.ExecuteAsync("INSERT INTO valorapesquisa.surveys(id,organization_id,form_version_id,name,title,status) SELECT @secondSurvey,@o,id,'Changed title','Changed title','draft' FROM valorapesquisa.form_versions ORDER BY created_at LIMIT 1; INSERT INTO valorapesquisa.action_plans(id,organization_id,diagnostic_id,title,summary,evidence_summary,expected_outcome,status) VALUES(@firstPlan,@o,@survey,'Same title','Summary','Evidence','Outcome','draft'),(@secondPlan,@o,@secondSurvey,'Same title','Summary','Evidence','Outcome','draft')", new { secondSurvey, o, firstPlan, secondPlan, survey });
            var plans = new ActionPlanRepository(factory, new DbTransactionFactory(factory));
            var filtered = await plans.List(o, Guid.NewGuid(), true, new ActionPlanListQuery(DiagnosticId: survey), default);
            Assert.Equal(firstPlan, Assert.Single(filtered.Items).Id);
            await db.ExecuteAsync("UPDATE valorapesquisa.surveys SET title='Renamed again' WHERE id=@survey", new { survey });
            Assert.Equal(firstPlan, Assert.Single((await plans.List(o, Guid.NewGuid(), true, new ActionPlanListQuery(DiagnosticId: survey), default)).Items).Id);
            var diagnosticCycle = Guid.NewGuid();
            await db.ExecuteAsync("INSERT INTO valorapesquisa.diagnostic_cycles(id,organization_id,survey_id,title,status) VALUES(@diagnosticCycle,@o,@survey,'Linked diagnostic','draft')", new { diagnosticCycle, o, survey });
            Assert.Equal(survey, await repository.DiagnosticSurvey(o, diagnosticCycle, default));
            Assert.Equal(firstPlan, Assert.Single((await plans.List(o, Guid.NewGuid(), true, new ActionPlanListQuery(DiagnosticId: diagnosticCycle), default)).Items).Id);
            await db.ExecuteAsync("INSERT INTO valorapesquisa.action_items(organization_id,action_plan_id,diagnostic_id,title,status,due_at) VALUES(@o,@firstPlan,NULL,'Direct plan activity','pending',now()-interval '1 day'),(@o,@secondPlan,@diagnosticCycle,'Linked activity','pending',now()-interval '1 day'),(@o,@secondPlan,NULL,'Unrelated sibling','pending',now()-interval '1 day')", new { o, firstPlan, secondPlan, diagnosticCycle });
            var contextual = await repository.Open(o, Guid.NewGuid(), request with { DiagnosticId = diagnosticCycle, Measurement = measurement with { Id = Guid.NewGuid() } }, default);
            await repository.Snapshot(o, Guid.NewGuid(), contextual, "Evidence", "Operational facts", "Next step", default);
            Assert.Equal(2, Assert.Single(await repository.Snapshots(o, contextual, default)).DelayedActions);
            var posterior = measurement with { Id = Guid.NewGuid(), Score = 10, ReferenceAt = measurement.ReferenceAt.AddDays(1), SourceJson = "{\"posterior\":true}" };
            var preview = EvolutionReevaluation.Preview(await repository.Version(o, id, default), measurement, posterior);
            var commandId = Guid.NewGuid().ToString("N");
            var competing = await Task.WhenAll(
                repository.Reevaluate(o, Guid.NewGuid(), id, preview, commandId, default),
                repository.Reevaluate(o, Guid.NewGuid(), id, preview, commandId, default));
            var registered = competing[0];
            Assert.Equal(registered, competing[1]);
            Assert.Equal(registered, await repository.Reevaluate(o, Guid.NewGuid(), id, preview, commandId, default));
            Assert.Equal(registered, await repository.ReevaluationReplay(o, id, commandId, preview.Fingerprint, posterior.SurveyId, preview.Version, default));
            Assert.Equal(10, Assert.Single(await repository.Snapshots(o, id, default)).Delta);
            Assert.Equal(measurement, await repository.Baseline(o, id, default));
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.Reevaluate(o, Guid.NewGuid(), id, preview, Guid.NewGuid().ToString("N"), default));
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.ReevaluationReplay(o, id, commandId, new string('a', 64), posterior.SurveyId, preview.Version, default));
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.ReevaluationReplay(o, id, commandId, preview.Fingerprint, Guid.NewGuid(), preview.Version, default));
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CloseReviewed(o, Guid.NewGuid(), id, 1, "Learned", "Pending", default));
            await repository.CloseReviewed(o, Guid.NewGuid(), id, 2, "Learned", "Plans remain open", default);
            Assert.Equal("closed", (await repository.Get(o, id, default))!.Status);
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.Snapshot(o, Guid.NewGuid(), id, "Late evidence", "Late interpretation", "Next step", default));
            Assert.Single(await repository.Snapshots(o, id, default));
            Assert.Equal(3, (await intelligence.ListModuleRecordsAsync(o, "evolution-cycles", default)).Single(x => x.Id == id).Version);
            Assert.Equal("draft", (await plans.Get(o, Guid.NewGuid(), firstPlan, true, default))!.Status);
            Assert.Equal(3, await db.ExecuteScalarAsync<int>("SELECT count(*)::int FROM valorapesquisa.action_items WHERE organization_id=@o AND status='pending'", new { o }));
            await Assert.ThrowsAsync<ArgumentException>(() => repository.Open(Guid.NewGuid(), Guid.NewGuid(), request, default));
            await db.ExecuteAsync("UPDATE valorapesquisa.evolution_measurements SET metadata_json='{}'::jsonb WHERE id=@Id AND organization_id=@o", new { measurement.Id, o });
            Assert.Null(await repository.Baseline(o, id, default));
            Assert.Equal(0, (await repository.Get(o, id, default))!.BaselineScore);
        }
        finally {
            await db.ExecuteAsync("DELETE FROM valorapesquisa.journey_events WHERE organization_id=@o; DELETE FROM valorapesquisa.evolution_snapshots WHERE organization_id=@o; DELETE FROM valorapesquisa.action_items WHERE organization_id=@o; DELETE FROM valorapesquisa.action_plans WHERE organization_id=@o; DELETE FROM valorapesquisa.organization_modules WHERE organization_id=@o; UPDATE valorapesquisa.evolution_cycles SET baseline_measurement_id=NULL WHERE organization_id=@o; DELETE FROM valorapesquisa.evolution_measurements WHERE organization_id=@o; DELETE FROM valorapesquisa.evolution_cycles WHERE organization_id=@o; DELETE FROM valorapesquisa.dimension_scores WHERE result_score_id IN(SELECT rs.id FROM valorapesquisa.result_scores rs JOIN valorapesquisa.responses r ON r.id=rs.response_id WHERE r.organization_id=@o); DELETE FROM valorapesquisa.result_scores WHERE response_id IN(SELECT id FROM valorapesquisa.responses WHERE organization_id=@o); DELETE FROM valorapesquisa.responses WHERE organization_id=@o; DELETE FROM valorapesquisa.diagnostic_cycles WHERE organization_id=@o; UPDATE valorapesquisa.surveys SET deleted_at=now() WHERE organization_id=@o", new { o });
        }
    }
}
