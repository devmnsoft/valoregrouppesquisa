using Dapper;
using Valora.Application.Contracts;
using Valora.Application.Evolution;
namespace Valora.Infrastructure.Repositories;

public sealed class EvolutionRepository(IDbConnectionFactory db, IDbTransactionFactory tx) : IEvolutionRepository {
    public async Task<Guid> ReviewReference(Guid o, Guid u, Guid cycle, long version, string evidence, string nextStep, string commandId, CancellationToken c) {
        await using var z = await tx.BeginAsync(c);
        var state = await z.Connection.QuerySingleOrDefaultAsync<LockedCycle>(new CommandDefinition("SELECT status,version,baseline_measurement_id BaselineMeasurementId FROM valorapesquisa.evolution_cycles WHERE id=@cycle AND organization_id=@o AND deleted_at IS NULL FOR UPDATE", new { o, cycle }, z.Transaction, cancellationToken: c)) ?? throw new KeyNotFoundException("Ciclo não encontrado.");
        var fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new { version, evidence, nextStep }))));
        var previous = await z.Connection.QuerySingleOrDefaultAsync<ReviewReplay>(new CommandDefinition("SELECT id,metadata_json->>'fingerprint' Fingerprint FROM valorapesquisa.evolution_snapshots WHERE organization_id=@o AND evolution_cycle_id=@cycle AND metadata_json->>'commandId'=@commandId", new { o, cycle, commandId }, z.Transaction, cancellationToken: c));
        if (previous is not null) {
            if (previous.Fingerprint != fingerprint) throw new InvalidOperationException("Este comando já foi utilizado para outra revisão.");
            await z.CommitAsync(); return previous.Id;
        }
        var source = await z.Connection.QuerySingleAsync<string>(new CommandDefinition("SELECT BaselineSource FROM (" + Cycles + ") source WHERE id=@cycle", new { cycle }, z.Transaction, cancellationToken: c));
        if (state.Status is "closed" or "archived" || state.Version != version || source != "legacy_review_required") throw new InvalidOperationException("O ciclo mudou, está encerrado ou não possui referência histórica pendente.");
        var id = Guid.NewGuid();
        var metadata = System.Text.Json.JsonSerializer.Serialize(new { commandId, fingerprint, version, outcome = "inconclusive", reviewedBy = u });
        await z.Connection.ExecuteAsync(new CommandDefinition("""
INSERT INTO valorapesquisa.evolution_snapshots(id,organization_id,evolution_cycle_id,snapshot_type,trend,evidence_summary,interpretation,recommendation,metadata_json,created_by_user_id)
SELECT @id,@o,@cycle,'reference_review','insufficient_sample',@evidence,'Origem histórica ainda não comprovada. Valores e referências originais preservados.',@nextStep,
CAST(@metadata AS jsonb)||jsonb_build_object('originalReference',jsonb_build_object('diagnosticId',diagnostic_id,'resultId',result_id,'surveyId',survey_id,'cycleId',cycle_id,'baselineScore',baseline_score,'currentScore',current_score,'metadata',metadata_json)),@u
FROM valorapesquisa.evolution_cycles WHERE id=@cycle AND organization_id=@o;
UPDATE valorapesquisa.evolution_cycles SET version=version+1,updated_at=now(),metadata_json=metadata_json||jsonb_build_object('referenceReviewRequired',true,'lastReferenceReviewId',@id::text) WHERE id=@cycle AND organization_id=@o;
INSERT INTO valorapesquisa.journey_events(organization_id,diagnostic_id,result_id,survey_id,cycle_id,event_type,title,description,source_type,source_id,evidence_summary,created_by_user_id)
SELECT @o,diagnostic_id,result_id,survey_id,cycle_id,'evolution_snapshot','Referência histórica revisada',@nextStep,'evolution',@id,@evidence,@u FROM valorapesquisa.evolution_cycles WHERE id=@cycle AND organization_id=@o;
""", new { id, o, u, cycle, evidence, nextStep, metadata }, z.Transaction, cancellationToken: c));
        await z.CommitAsync(); return id;
    }
    private sealed record ReviewReplay(Guid Id, string Fingerprint);
    public async Task<Guid?> DiagnosticSurvey(Guid o, Guid diagnosticId, CancellationToken c) {
        using var x = db.Create();
        return await x.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition("SELECT s.id FROM valorapesquisa.surveys s WHERE s.organization_id=@o AND s.deleted_at IS NULL AND (s.id=@diagnosticId OR EXISTS(SELECT 1 FROM valorapesquisa.diagnostic_cycles dc WHERE dc.id=@diagnosticId AND dc.survey_id=s.id AND dc.organization_id=s.organization_id AND dc.deleted_at IS NULL))", new { o, diagnosticId }, cancellationToken: c));
    }
    public async Task<Guid?> ResultSurvey(Guid o, Guid resultId, CancellationToken c) {
        using var x = db.Create();
        return await x.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition("SELECT r.survey_id FROM valorapesquisa.results individual JOIN valorapesquisa.responses r ON r.id=individual.response_id AND r.organization_id=individual.organization_id JOIN valorapesquisa.surveys s ON s.id=r.survey_id AND s.organization_id=r.organization_id WHERE individual.id=@resultId AND individual.organization_id=@o AND s.deleted_at IS NULL", new { o, resultId }, cancellationToken: c));
    }
    public async Task<bool> LinksAvailable(Guid o, Guid? diagnosticId, Guid? governanceCycleId, CancellationToken c) {
        using var x = db.Create();
        return await x.ExecuteScalarAsync<bool>(new CommandDefinition("SELECT (@diagnosticId IS NULL OR EXISTS(SELECT 1 FROM valorapesquisa.surveys WHERE organization_id=@o AND deleted_at IS NULL AND (id=@diagnosticId OR id IN(SELECT survey_id FROM valorapesquisa.diagnostic_cycles WHERE id=@diagnosticId AND organization_id=@o AND deleted_at IS NULL)))) AND (@governanceCycleId IS NULL OR EXISTS(SELECT 1 FROM valorapesquisa.organizational_governance_cycles WHERE id=@governanceCycleId AND organization_id=@o AND deleted_at IS NULL))", new { o, diagnosticId, governanceCycleId }, cancellationToken: c));
    }
    private sealed record LockedCycle(string Status, long Version, Guid? BaselineMeasurementId);
    private sealed record ReplayRow(Guid Id, string Fingerprint, Guid SurveyId, long Version);

    public async Task<long> Version(Guid o, Guid cycle, CancellationToken c) {
        using var x = db.Create();
        return await x.QuerySingleAsync<long>(new CommandDefinition("SELECT version FROM valorapesquisa.evolution_cycles WHERE id=@cycle AND organization_id=@o AND deleted_at IS NULL", new { o, cycle }, cancellationToken: c));
    }

    public async Task<Guid?> ReevaluationReplay(Guid o, Guid cycle, string commandId, string fingerprint, Guid surveyId, long version, CancellationToken c) {
        using var x = db.Create();
        return await Replay(x, null, o, cycle, commandId, fingerprint, surveyId, version, c);
    }

    private static async Task<Guid?> Replay(System.Data.IDbConnection x, System.Data.IDbTransaction? transaction, Guid o, Guid cycle, string commandId, string fingerprint, Guid surveyId, long version, CancellationToken c) {
        var row = await x.QuerySingleOrDefaultAsync<ReplayRow>(new CommandDefinition("SELECT id,metadata_json->>'fingerprint' Fingerprint,(metadata_json->'Current'->>'SurveyId')::uuid SurveyId,coalesce((metadata_json->>'version')::bigint,-1) Version FROM valorapesquisa.evolution_snapshots WHERE organization_id=@o AND evolution_cycle_id=@cycle AND metadata_json->>'commandId'=@commandId", new { o, cycle, commandId }, transaction, cancellationToken: c));
        if (row is null) return null;
        if (row.Fingerprint != fingerprint || row.SurveyId != surveyId || row.Version != version) throw new InvalidOperationException("Este comando já foi utilizado para outra confirmação.");
        return row.Id;
    }

    public async Task<Guid> Reevaluate(Guid o, Guid u, Guid cycle, EvolutionReevaluationPreview preview, string commandId, CancellationToken c) {
        await using var z = await tx.BeginAsync(c);
        var state = await z.Connection.QuerySingleOrDefaultAsync<LockedCycle>(new CommandDefinition("SELECT status,version,baseline_measurement_id BaselineMeasurementId FROM valorapesquisa.evolution_cycles WHERE id=@cycle AND organization_id=@o AND deleted_at IS NULL FOR UPDATE", new { o, cycle }, z.Transaction, cancellationToken: c)) ?? throw new KeyNotFoundException("Ciclo não encontrado.");
        var replay = await Replay(z.Connection, z.Transaction, o, cycle, commandId, preview.Fingerprint, preview.Current.SurveyId, preview.Version, c);
        if (replay.HasValue) { await z.CommitAsync(); return replay.Value; }
        if (state.Status is "closed" or "archived" || state.Version != preview.Version || state.BaselineMeasurementId != preview.Baseline.Id)
            throw new InvalidOperationException("O ciclo mudou. Atualize a prévia antes de confirmar.");
        var id = Guid.NewGuid();
        await SaveMeasurement(z.Connection, z.Transaction, o, cycle, preview.Current, c);
        var metadata = System.Text.Json.JsonSerializer.Serialize(new { commandId, fingerprint = preview.Fingerprint, version = preview.Version, preview.Baseline, preview.Current, preview.Comparison });
        await z.Connection.ExecuteAsync(new CommandDefinition("""
INSERT INTO valorapesquisa.evolution_snapshots(id,organization_id,evolution_cycle_id,snapshot_type,score,previous_score,delta,trend,evidence_summary,interpretation,recommendation,metadata_json,created_by_user_id)
VALUES(@id,@o,@cycle,'reevaluation',@score,@previous,@delta,'insufficient_sample',@evidence,@interpretation,'Analise a mudança observada e suas limitações.',CAST(@metadata AS jsonb),@u);
UPDATE valorapesquisa.evolution_cycles SET status='reviewed',current_score=CASE WHEN @comparable THEN @score ELSE current_score END,version=version+1,updated_at=now() WHERE id=@cycle AND organization_id=@o;
INSERT INTO valorapesquisa.journey_events(organization_id,diagnostic_id,result_id,survey_id,cycle_id,event_type,title,description,source_type,source_id,evidence_summary,created_by_user_id)
SELECT @o,diagnostic_id,result_id,survey_id,cycle_id,'evolution_snapshot','Reavaliação registrada',@interpretation,'evolution',@id,@evidence,@u FROM valorapesquisa.evolution_cycles WHERE id=@cycle AND organization_id=@o;
""", new {
            id,
            o,
            u,
            cycle,
            score = preview.Current.Score,
            previous = preview.Baseline.Score,
            delta = preview.Comparison.AbsoluteVariation,
            comparable = preview.Comparison.IsComparable,
            evidence = preview.Current.Limitation,
            interpretation = preview.Comparison.Limitation,
            metadata
        }, z.Transaction, cancellationToken: c));
        await z.CommitAsync();
        return id;
    }

    public async Task CloseReviewed(Guid o, Guid u, Guid cycle, long version, string learning, string pendingTreatment, CancellationToken c) {
        await using var z = await tx.BeginAsync(c);
        var state = await z.Connection.QuerySingleOrDefaultAsync<LockedCycle>(new CommandDefinition("SELECT status,version,baseline_measurement_id BaselineMeasurementId FROM valorapesquisa.evolution_cycles WHERE id=@cycle AND organization_id=@o AND deleted_at IS NULL FOR UPDATE", new { o, cycle }, z.Transaction, cancellationToken: c)) ?? throw new KeyNotFoundException("Ciclo não encontrado.");
        if (state.Status is "closed" or "archived" || state.Version != version) throw new InvalidOperationException("O ciclo mudou ou está encerrado. Revise os dados atuais.");
        await z.Connection.ExecuteAsync(new CommandDefinition("""
UPDATE valorapesquisa.evolution_cycles SET status='closed',learning_summary=@learning,period_end=coalesce(period_end,((now() AT TIME ZONE (SELECT time_zone FROM valorapesquisa.organizations WHERE id=@o))::date)::timestamp AT TIME ZONE 'UTC'),version=version+1,updated_at=now(),metadata_json=metadata_json||jsonb_build_object('pendingTreatment',@pendingTreatment) WHERE id=@cycle AND organization_id=@o;
INSERT INTO valorapesquisa.journey_events(organization_id,diagnostic_id,result_id,survey_id,cycle_id,event_type,title,description,source_type,source_id,evidence_summary,created_by_user_id)
SELECT @o,diagnostic_id,result_id,survey_id,cycle_id,'cycle_closed','Ciclo de evolução encerrado',@learning,'evolution',@cycle,@pendingTreatment,@u FROM valorapesquisa.evolution_cycles WHERE id=@cycle AND organization_id=@o;
""", new { o, u, cycle, learning, pendingTreatment }, z.Transaction, cancellationToken: c));
        await z.CommitAsync();
    }
    const string Cycles = "SELECT id,diagnostic_id DiagnosticId,result_id ResultId,title,summary,status,baseline_score BaselineScore,current_score CurrentScore,target_score TargetScore,baseline_level BaselineLevel,current_level CurrentLevel,trend,period_start PeriodStart,period_end PeriodEnd,evidence_summary EvidenceSummary,learning_summary LearningSummary,CASE WHEN baseline_measurement_id IS NOT NULL THEN 'organizational_measurement' WHEN metadata_json->>'baselineSource' IN('manual','manual_informed') AND (result_id IS NULL OR metadata_json->>'referenceSchemaVersion'='2') THEN 'manual_informed' ELSE 'legacy_review_required' END BaselineSource,metadata_json->>'methodologyVersion' MethodologyVersion,created_at CreatedAt,metadata_json->>'pendingTreatment' PendingTreatment FROM valorapesquisa.evolution_cycles";
    public async Task<IReadOnlyList<EvolutionCycleDto>> List(Guid o, CancellationToken c) { using var x = db.Create(); return (await x.QueryAsync<EvolutionCycleDto>(new CommandDefinition(Cycles + " WHERE organization_id=@o AND deleted_at IS NULL ORDER BY created_at DESC", new { o }, cancellationToken: c))).ToList(); }
    public async Task<EvolutionCycleDto?> Get(Guid o, Guid id, CancellationToken c) { using var x = db.Create(); return await x.QuerySingleOrDefaultAsync<EvolutionCycleDto>(new CommandDefinition(Cycles + " WHERE organization_id=@o AND id=@id AND deleted_at IS NULL", new { o, id }, cancellationToken: c)); }
    public async Task<IReadOnlyList<EvolutionSnapshotDto>> Snapshots(Guid o, Guid cycle, CancellationToken c) { const string q = "SELECT id,evolution_cycle_id EvolutionCycleId,snapshot_type SnapshotType,score,previous_score PreviousScore,delta,level,trend,completed_actions CompletedActions,delayed_actions DelayedActions,open_alerts OpenAlerts,evidence_summary EvidenceSummary,interpretation,recommendation,calculated_at CalculatedAt,metadata_json::text MetadataJson FROM valorapesquisa.evolution_snapshots WHERE organization_id=@o AND evolution_cycle_id=@cycle AND deleted_at IS NULL ORDER BY calculated_at DESC"; using var x = db.Create(); return (await x.QueryAsync<EvolutionSnapshotDto>(new CommandDefinition(q, new { o, cycle }, cancellationToken: c))).ToList(); }
    public async Task<Guid> Open(Guid o, Guid u, OpenEvolutionCycleRequest r, CancellationToken c) {
        var id = Guid.NewGuid();
        await using var z = await tx.BeginAsync(c);
        const string validate = """
SELECT (@DiagnosticId IS NULL OR EXISTS(SELECT 1 FROM valorapesquisa.surveys s WHERE s.organization_id=@o AND s.deleted_at IS NULL AND (s.id=@DiagnosticId OR EXISTS(SELECT 1 FROM valorapesquisa.diagnostic_cycles dc WHERE dc.id=@DiagnosticId AND dc.survey_id=s.id AND dc.organization_id=@o AND dc.deleted_at IS NULL))))
 AND (@GovernanceCycleId IS NULL OR EXISTS(SELECT 1 FROM valorapesquisa.organizational_governance_cycles WHERE id=@GovernanceCycleId AND organization_id=@o AND deleted_at IS NULL))
""";
        if (!await z.Connection.ExecuteScalarAsync<bool>(new CommandDefinition(validate, new { o, r.DiagnosticId, r.GovernanceCycleId }, z.Transaction, cancellationToken: c)))
            throw new ArgumentException("Os vínculos não estão disponíveis nesta organização.");
        var metadata = System.Text.Json.JsonSerializer.Serialize(new { referenceSchemaVersion = 2, baselineSource = r.BaselineSource, methodologyVersion = r.MethodologyVersion });
        await z.Connection.ExecuteAsync(new CommandDefinition("""
INSERT INTO valorapesquisa.evolution_cycles(id,organization_id,diagnostic_id,result_id,governance_cycle_id,title,summary,status,baseline_score,current_score,target_score,baseline_level,current_level,trend,period_start,period_end,evidence_summary,metadata_json,created_by_user_id,survey_id,cycle_id)
SELECT @id,@o,@DiagnosticId,@ResultId,@GovernanceCycleId,@Title,@Summary,'open',@BaselineScore,@BaselineScore,@TargetScore,@BaselineLevel,@BaselineLevel,'insufficient_sample',@PeriodStart,@PeriodEnd,@EvidenceSummary,CAST(@metadata AS jsonb),@u,s.id,dc.id
FROM (SELECT 1) anchor LEFT JOIN valorapesquisa.surveys s ON s.organization_id=@o AND s.deleted_at IS NULL AND (s.id=@SourceSurveyId OR @SourceSurveyId IS NULL AND (s.id=@DiagnosticId OR s.id IN(SELECT survey_id FROM valorapesquisa.diagnostic_cycles WHERE id=@DiagnosticId AND organization_id=@o AND deleted_at IS NULL)))
LEFT JOIN valorapesquisa.diagnostic_cycles dc ON dc.survey_id=s.id AND dc.organization_id=@o AND dc.deleted_at IS NULL
""", new {
            id,
            o,
            u,
            r.DiagnosticId,
            r.SourceSurveyId,
            r.ResultId,
            r.GovernanceCycleId,
            r.Title,
            r.Summary,
            r.BaselineScore,
            r.TargetScore,
            r.BaselineLevel,
            PeriodStart = DateTime.SpecifyKind(r.PeriodStart.Date, DateTimeKind.Utc),
            PeriodEnd = r.PeriodEnd is { } end ? (DateTime?)DateTime.SpecifyKind(end.Date, DateTimeKind.Utc) : null,
            r.EvidenceSummary,
            metadata
        }, z.Transaction, cancellationToken: c));
        if (r.Measurement is { } measurement) {
            await SaveMeasurement(z.Connection, z.Transaction, o, id, measurement, c);
            await z.Connection.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.evolution_cycles SET baseline_measurement_id=@measurementId WHERE id=@id AND organization_id=@o", new { id, o, measurementId = measurement.Id }, z.Transaction, cancellationToken: c));
        }
        await z.CommitAsync();
        return id;
    }

    private static Task SaveMeasurement(System.Data.IDbConnection connection, System.Data.IDbTransaction transaction, Guid o, Guid cycle, PreservedEvolutionMeasurement m, CancellationToken c) =>
        connection.ExecuteAsync(new CommandDefinition("""
INSERT INTO valorapesquisa.evolution_measurements(id,organization_id,evolution_cycle_id,index_code,score,evidence_summary,measured_at,metadata_json)
VALUES(@Id,@o,@cycle,'maturity',@Score,@Limitation,@ReferenceAt,CAST(@json AS jsonb))
""", new { m.Id, o, cycle, m.Score, m.Limitation, ReferenceAt = DateTime.SpecifyKind(m.ReferenceAt, DateTimeKind.Utc), json = System.Text.Json.JsonSerializer.Serialize(m) }, transaction, cancellationToken: c));

    public async Task<PreservedEvolutionMeasurement?> Baseline(Guid o, Guid cycle, CancellationToken c) {
        using var x = db.Create();
        var json = await x.QuerySingleOrDefaultAsync<string>(new CommandDefinition("""
SELECT m.metadata_json::text FROM valorapesquisa.evolution_cycles ec
JOIN valorapesquisa.evolution_measurements m ON m.id=ec.baseline_measurement_id AND m.organization_id=ec.organization_id
WHERE ec.organization_id=@o AND ec.id=@cycle AND ec.deleted_at IS NULL
""", new { o, cycle }, cancellationToken: c));
        return json is null ? null : System.Text.Json.JsonSerializer.Deserialize<PreservedEvolutionMeasurement>(json);
    }
    public async Task<Guid> Snapshot(Guid o, Guid u, Guid cycle, string evidence, string interpretation, string recommendation, CancellationToken c) { var id = Guid.NewGuid(); await using var z = await tx.BeginAsync(c); var state = await z.Connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition("SELECT status FROM valorapesquisa.evolution_cycles WHERE id=@cycle AND organization_id=@o AND deleted_at IS NULL FOR UPDATE", new { o, cycle }, z.Transaction, cancellationToken: c)); if (state is null) throw new KeyNotFoundException("Ciclo não encontrado."); if (state is "closed" or "archived") throw new InvalidOperationException("Um ciclo encerrado não aceita novas leituras."); const string q = """
WITH cyc AS (
  SELECT id,current_score,baseline_level,diagnostic_id,result_id,survey_id,cycle_id FROM valorapesquisa.evolution_cycles WHERE id=@cycle AND organization_id=@o
), direct_plans AS (
  SELECT p.id FROM valorapesquisa.action_plans p CROSS JOIN cyc
   WHERE p.organization_id=@o AND p.deleted_at IS NULL
     AND ((cyc.diagnostic_id IS NULL AND cyc.result_id IS NULL)
          OR p.diagnostic_id IN(cyc.diagnostic_id,cyc.survey_id,cyc.cycle_id) OR (p.origin_type='diagnostic' AND p.origin_id IN(cyc.diagnostic_id,cyc.survey_id,cyc.cycle_id))
          OR p.result_id=cyc.result_id OR (p.origin_type='result' AND p.origin_id=cyc.result_id))
), linked_items AS (
  SELECT i.id,i.action_plan_id FROM valorapesquisa.action_items i JOIN valorapesquisa.action_plans parent ON parent.id=i.action_plan_id AND parent.organization_id=i.organization_id AND parent.deleted_at IS NULL CROSS JOIN cyc
  WHERE i.organization_id=@o AND i.deleted_at IS NULL
    AND ((cyc.diagnostic_id IS NULL AND cyc.result_id IS NULL)
         OR i.diagnostic_id IN(cyc.diagnostic_id,cyc.survey_id,cyc.cycle_id) OR (i.origin_type='diagnostic' AND i.origin_id IN(cyc.diagnostic_id,cyc.survey_id,cyc.cycle_id))
         OR i.result_id=cyc.result_id OR (i.origin_type='result' AND i.origin_id=cyc.result_id))
), scoped_plans AS (
  SELECT id FROM direct_plans
  UNION
  SELECT action_plan_id FROM linked_items
), scoped_items AS (
  SELECT i.status,i.due_at FROM valorapesquisa.action_items i
  WHERE i.organization_id=@o AND i.deleted_at IS NULL
    AND (i.action_plan_id IN (SELECT id FROM direct_plans) OR i.id IN (SELECT id FROM linked_items))
), stats AS (
  SELECT count(*) FILTER(WHERE i.status='completed')::int completed,
         count(*) FILTER(WHERE i.due_at<now() AND i.status NOT IN('completed','canceled'))::int delayed
  FROM scoped_items i
), alerts AS (
  SELECT count(*)::int opened FROM valorapesquisa.intelligent_alerts a CROSS JOIN cyc
  WHERE a.organization_id=@o AND a.deleted_at IS NULL AND a.status<>'resolved'
    AND ((cyc.diagnostic_id IS NULL AND cyc.result_id IS NULL AND cyc.survey_id IS NULL) OR a.survey_id=cyc.survey_id OR a.cycle_id=cyc.cycle_id OR a.survey_id=cyc.diagnostic_id OR a.cycle_id=cyc.diagnostic_id)
)
INSERT INTO valorapesquisa.evolution_snapshots(id,organization_id,evolution_cycle_id,diagnostic_id,result_id,snapshot_type,score,previous_score,delta,level,trend,completed_actions,delayed_actions,open_alerts,evidence_summary,interpretation,recommendation,calculated_at,created_by_user_id)
SELECT @id,@o,@cycle,cyc.diagnostic_id,cyc.result_id,'monitoring',cyc.current_score,NULL,NULL,cyc.baseline_level,'insufficient_sample',stats.completed,stats.delayed,alerts.opened,@evidence,@interpretation,@recommendation,now(),@u FROM stats CROSS JOIN alerts CROSS JOIN cyc
"""; var inserted = await z.Connection.ExecuteAsync(new CommandDefinition(q, new { id, o, u, cycle, evidence, interpretation, recommendation }, z.Transaction, cancellationToken: c)); if (inserted != 1) throw new InvalidOperationException("Não foi possível preservar a leitura do ciclo."); await z.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.journey_events(organization_id,diagnostic_id,result_id,survey_id,cycle_id,event_type,title,description,source_type,source_id,impact_level,evidence_summary,occurred_at,created_by_user_id) SELECT @o,diagnostic_id,result_id,survey_id,cycle_id,'evolution_snapshot','Snapshot de evolução registrado','Execução de ações registrada separadamente; não implica variação metodológica nem causalidade.','evolution',@id,'medium',@evidence,now(),@u FROM valorapesquisa.evolution_cycles WHERE id=@cycle AND organization_id=@o", new { id, o, u, cycle, evidence }, z.Transaction, cancellationToken: c)); await z.Connection.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.evolution_cycles SET version=version+1,updated_at=now() WHERE id=@cycle AND organization_id=@o", new { o, cycle }, z.Transaction, cancellationToken: c)); await z.CommitAsync(); return id; }
    public Task Close(Guid o, Guid u, Guid cycle, string learning, CancellationToken c) => throw new ArgumentException("O encerramento exige uma revisão com versão e tratamento das pendências.");
}
