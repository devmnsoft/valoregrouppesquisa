using Dapper;
using Valora.Application.Contracts;
using Valora.Application.Evolution;
namespace Valora.Infrastructure.Repositories;

public sealed class EvolutionRepository(IDbConnectionFactory db, IDbTransactionFactory tx) : IEvolutionRepository {
    const string Cycles = "SELECT id,diagnostic_id DiagnosticId,result_id ResultId,title,summary,status,baseline_score BaselineScore,current_score CurrentScore,target_score TargetScore,baseline_level BaselineLevel,current_level CurrentLevel,trend,period_start PeriodStart,period_end PeriodEnd,evidence_summary EvidenceSummary,learning_summary LearningSummary,coalesce(metadata_json->>'baselineSource',CASE WHEN result_id IS NULL THEN 'manual_informed' ELSE 'organizational_measurement' END) BaselineSource,metadata_json->>'methodologyVersion' MethodologyVersion,created_at CreatedAt FROM valorapesquisa.evolution_cycles";
    public async Task<IReadOnlyList<EvolutionCycleDto>> List(Guid o, CancellationToken c) { using var x = db.Create(); return (await x.QueryAsync<EvolutionCycleDto>(new CommandDefinition(Cycles + " WHERE organization_id=@o AND deleted_at IS NULL ORDER BY created_at DESC", new { o }, cancellationToken: c))).ToList(); }
    public async Task<EvolutionCycleDto?> Get(Guid o, Guid id, CancellationToken c) { using var x = db.Create(); return await x.QuerySingleOrDefaultAsync<EvolutionCycleDto>(new CommandDefinition(Cycles + " WHERE organization_id=@o AND id=@id AND deleted_at IS NULL", new { o, id }, cancellationToken: c)); }
    public async Task<IReadOnlyList<EvolutionSnapshotDto>> Snapshots(Guid o, Guid cycle, CancellationToken c) { const string q = "SELECT id,evolution_cycle_id EvolutionCycleId,snapshot_type SnapshotType,score,previous_score PreviousScore,delta,level,trend,completed_actions CompletedActions,delayed_actions DelayedActions,open_alerts OpenAlerts,evidence_summary EvidenceSummary,interpretation,recommendation,calculated_at CalculatedAt FROM valorapesquisa.evolution_snapshots WHERE organization_id=@o AND evolution_cycle_id=@cycle AND deleted_at IS NULL ORDER BY calculated_at DESC"; using var x = db.Create(); return (await x.QueryAsync<EvolutionSnapshotDto>(new CommandDefinition(q, new { o, cycle }, cancellationToken: c))).ToList(); }
    public async Task<Guid> Open(Guid o, Guid u, OpenEvolutionCycleRequest r, CancellationToken c) { var id = Guid.NewGuid(); using var x = db.Create(); await x.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.evolution_cycles(id,organization_id,diagnostic_id,result_id,governance_cycle_id,title,summary,status,baseline_score,current_score,target_score,baseline_level,current_level,trend,period_start,period_end,evidence_summary,metadata_json,created_by_user_id) VALUES(@id,@o,@DiagnosticId,@ResultId,@GovernanceCycleId,@Title,@Summary,'open',@BaselineScore,@BaselineScore,@TargetScore,@BaselineLevel,@BaselineLevel,'insufficient_sample',@PeriodStart,@PeriodEnd,@EvidenceSummary,jsonb_build_object('baselineSource',@BaselineSource,'methodologyVersion',@MethodologyVersion,'populationSignature',@PopulationSignature,'dimensionSignature',@DimensionSignature,'scaleSignature',@ScaleSignature,'calculationCriteria',@CalculationCriteria,'measurementType',CASE WHEN @BaselineSource='organizational_measurement' THEN 'organizational' ELSE 'manual' END),@u)", new { id, o, u, r.DiagnosticId, r.ResultId, r.GovernanceCycleId, r.Title, r.Summary, r.BaselineScore, r.TargetScore, r.BaselineLevel, r.PeriodStart, r.PeriodEnd, r.EvidenceSummary, r.BaselineSource, r.MethodologyVersion, r.PopulationSignature, r.DimensionSignature, r.ScaleSignature, r.CalculationCriteria }, cancellationToken: c)); return id; }
    public async Task<Guid> Snapshot(Guid o, Guid u, Guid cycle, string evidence, string interpretation, string recommendation, CancellationToken c) { var id = Guid.NewGuid(); await using var z = await tx.BeginAsync(c); var state = await z.Connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition("SELECT status FROM valorapesquisa.evolution_cycles WHERE id=@cycle AND organization_id=@o AND deleted_at IS NULL FOR UPDATE", new { o, cycle }, z.Transaction, cancellationToken: c)); if (state is null) throw new KeyNotFoundException("Ciclo não encontrado."); if (state is "closed" or "archived") throw new InvalidOperationException("Um ciclo encerrado não aceita novas leituras."); const string q = """
WITH cyc AS (
  SELECT id,current_score,baseline_level,diagnostic_id,result_id FROM valorapesquisa.evolution_cycles WHERE id=@cycle AND organization_id=@o
), direct_plans AS (
  SELECT p.id FROM valorapesquisa.action_plans p CROSS JOIN cyc
   WHERE p.organization_id=@o AND p.deleted_at IS NULL
     AND ((cyc.diagnostic_id IS NULL AND cyc.result_id IS NULL)
          OR p.diagnostic_id=cyc.diagnostic_id OR (p.origin_type='diagnostic' AND p.origin_id=cyc.diagnostic_id)
          OR p.result_id=cyc.result_id OR (p.origin_type='result' AND p.origin_id=cyc.result_id))
), linked_items AS (
  SELECT i.id,i.action_plan_id FROM valorapesquisa.action_items i CROSS JOIN cyc
  WHERE i.organization_id=@o AND i.deleted_at IS NULL
    AND ((cyc.diagnostic_id IS NULL AND cyc.result_id IS NULL)
         OR i.diagnostic_id=cyc.diagnostic_id OR (i.origin_type='diagnostic' AND i.origin_id=cyc.diagnostic_id)
         OR i.result_id=cyc.result_id OR (i.origin_type='result' AND i.origin_id=cyc.result_id))
), scoped_plans AS (
  SELECT id FROM direct_plans
  UNION
  SELECT action_plan_id FROM linked_items
), scoped_items AS (
  SELECT i.* FROM valorapesquisa.action_items i
  WHERE i.organization_id=@o AND i.deleted_at IS NULL
    AND (i.action_plan_id IN (SELECT id FROM direct_plans) OR i.id IN (SELECT id FROM linked_items))
), stats AS (
  SELECT count(*) FILTER(WHERE i.status='completed')::int completed,
         count(*) FILTER(WHERE i.due_at<now() AND i.status NOT IN('completed','canceled'))::int delayed
  FROM scoped_items i
), alerts AS (
  SELECT count(*)::int opened FROM valorapesquisa.intelligent_alerts a CROSS JOIN cyc
  WHERE a.organization_id=@o AND a.deleted_at IS NULL AND a.status<>'resolved'
    AND (cyc.diagnostic_id IS NULL OR a.survey_id=cyc.diagnostic_id OR a.cycle_id=cyc.diagnostic_id)
)
INSERT INTO valorapesquisa.evolution_snapshots(id,organization_id,evolution_cycle_id,diagnostic_id,result_id,snapshot_type,score,previous_score,delta,level,trend,completed_actions,delayed_actions,open_alerts,evidence_summary,interpretation,recommendation,calculated_at,created_by_user_id)
SELECT @id,@o,@cycle,cyc.diagnostic_id,cyc.result_id,'monitoring',cyc.current_score,NULL,NULL,cyc.baseline_level,'insufficient_sample',stats.completed,stats.delayed,alerts.opened,@evidence,@interpretation,@recommendation,now(),@u FROM stats CROSS JOIN alerts CROSS JOIN cyc
"""; var inserted = await z.Connection.ExecuteAsync(new CommandDefinition(q, new { id, o, u, cycle, evidence, interpretation, recommendation }, z.Transaction, cancellationToken: c)); if (inserted != 1) throw new InvalidOperationException("Não foi possível preservar a leitura do ciclo."); await z.Connection.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.journey_events(organization_id,diagnostic_id,result_id,event_type,title,description,source_type,source_id,impact_level,evidence_summary,occurred_at,created_by_user_id) SELECT @o,diagnostic_id,result_id,'evolution_snapshot','Snapshot de evolução registrado','Execução de ações registrada separadamente; não implica variação metodológica nem causalidade.','evolution',@id,'medium',@evidence,now(),@u FROM valorapesquisa.evolution_cycles WHERE id=@cycle AND organization_id=@o", new { id, o, u, cycle, evidence }, z.Transaction, cancellationToken: c)); await z.CommitAsync(); return id; }
    public async Task Close(Guid o, Guid u, Guid cycle, string learning, CancellationToken c) { using var x = db.Create(); await x.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.evolution_cycles SET status='closed',learning_summary=@learning,period_end=coalesce(period_end,now()),updated_at=now() WHERE id=@cycle AND organization_id=@o AND deleted_at IS NULL", new { o, u, cycle, learning }, cancellationToken: c)); }
}
