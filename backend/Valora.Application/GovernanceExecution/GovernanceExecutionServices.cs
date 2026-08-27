using System.ComponentModel.DataAnnotations;

namespace Valora.Application.GovernanceExecution;

public sealed record GovernanceCommand(
    Guid OrganizationId,
    Guid ActorUserId,
    string EventType,
    string EntityType,
    Guid EntityId,
    IReadOnlyDictionary<string, object?> Values);

public interface IGovernanceExecutionStore
{
    Task ExecuteAsync(GovernanceCommand command, CancellationToken cancellationToken);
}

public interface IGovernanceCommandService
{
    Task<Guid> CreateAsync(Guid organizationId, Guid actorUserId, IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken = default);
}

public abstract class GovernanceCommandService(IGovernanceExecutionStore store, string entityType, string eventType)
    : IGovernanceCommandService
{
    public async Task<Guid> CreateAsync(Guid organizationId, Guid actorUserId,
        IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty) throw new ValidationException("Selecione uma organização válida.");
        if (actorUserId == Guid.Empty) throw new ValidationException("Não foi possível identificar o usuário responsável.");
        Validate(values);
        var id = Guid.NewGuid();
        await store.ExecuteAsync(new(organizationId, actorUserId, eventType, entityType, id, values), cancellationToken);
        return id;
    }

    protected static string Required(IReadOnlyDictionary<string, object?> values, string key, string label)
    {
        var value = values.TryGetValue(key, out var candidate) ? candidate?.ToString()?.Trim() : null;
        return !string.IsNullOrWhiteSpace(value) ? value : throw new ValidationException($"{label} é obrigatório.");
    }

    protected virtual void Validate(IReadOnlyDictionary<string, object?> values) { }
}

public interface IGovernanceCycleService : IGovernanceCommandService { }
public sealed class GovernanceCycleService(IGovernanceExecutionStore store)
    : GovernanceCommandService(store, "governance_cycle", "governance.cycle.created"), IGovernanceCycleService
{
    protected override void Validate(IReadOnlyDictionary<string, object?> v)
    { Required(v, "objective", "Objetivo"); Required(v, "period_start", "Início do período"); Required(v, "period_end", "Fim do período"); }
}

public interface IGovernanceMeetingService : IGovernanceCommandService { }
public sealed class GovernanceMeetingService(IGovernanceExecutionStore store)
    : GovernanceCommandService(store, "governance_meeting", "governance.meeting.created"), IGovernanceMeetingService
{ protected override void Validate(IReadOnlyDictionary<string, object?> v) { Required(v, "title", "Título"); Required(v, "scheduled_at", "Data da reunião"); } }

public interface IGovernanceDecisionService : IGovernanceCommandService { }
public sealed class GovernanceDecisionService(IGovernanceExecutionStore store)
    : GovernanceCommandService(store, "governance_decision", "governance.decision.created"), IGovernanceDecisionService
{
    protected override void Validate(IReadOnlyDictionary<string, object?> v)
    {
        Required(v, "title", "Título"); Required(v, "context", "Contexto"); Required(v, "responsible_user_id", "Responsável");
        Required(v, "status", "Status"); Required(v, "justification", "Justificativa humana");
    }
}

public interface IDecisionEvidenceService : IGovernanceCommandService { }
public sealed class DecisionEvidenceService(IGovernanceExecutionStore store)
    : GovernanceCommandService(store, "decision_evidence_link", "decision.evidence.linked"), IDecisionEvidenceService
{ protected override void Validate(IReadOnlyDictionary<string, object?> v) { Required(v, "decision_id", "Decisão"); Required(v, "evidence_id", "Evidência"); } }

public interface IActionPriorityService { string Resolve(DateTime dueAt, string impact); }
public sealed class ActionPriorityService : IActionPriorityService
{ public string Resolve(DateTime dueAt, string impact) => impact.Equals("critical", StringComparison.OrdinalIgnoreCase) || dueAt <= DateTime.UtcNow.AddDays(2) ? "critical" : dueAt <= DateTime.UtcNow.AddDays(7) ? "high" : "medium"; }

public interface IEvolutionMilestoneService : IGovernanceCommandService { }
public sealed class EvolutionMilestoneService(IGovernanceExecutionStore s) : GovernanceCommandService(s, "evolution_milestone", "evolution.milestone.reached"), IEvolutionMilestoneService
{ protected override void Validate(IReadOnlyDictionary<string, object?> v) { Required(v, "title", "Marco"); Required(v, "evidence_summary", "Evidência do marco"); } }
public interface IEvolutionMetricSnapshotService : IGovernanceCommandService { }
public sealed class EvolutionMetricSnapshotService(IGovernanceExecutionStore s) : GovernanceCommandService(s, "evolution_metric_snapshot", "evolution.metric.measured"), IEvolutionMetricSnapshotService
{ protected override void Validate(IReadOnlyDictionary<string, object?> v) { Required(v, "metric_name", "Indicador"); Required(v, "source", "Fonte"); Required(v, "value", "Valor"); } }
public interface IOrganizationalLearningService : IGovernanceCommandService { }
public sealed class OrganizationalLearningService(IGovernanceExecutionStore s) : GovernanceCommandService(s, "organizational_learning_event", "organizational.learning.recorded"), IOrganizationalLearningService
{ protected override void Validate(IReadOnlyDictionary<string, object?> v) { Required(v, "learning", "Aprendizado"); Required(v, "evidence_summary", "Evidência"); } }

public interface IOneOnOneActionLinkService : IGovernanceCommandService { }
public sealed class OneOnOneActionLinkService(IGovernanceExecutionStore s) : GovernanceCommandService(s, "one_on_one_action_link", "one_on_one.action.linked"), IOneOnOneActionLinkService
{ protected override void Validate(IReadOnlyDictionary<string, object?> v) { Required(v, "session_id", "Sessão"); Required(v, "action_item_id", "Ação"); } }

public interface IIndicatorTargetService : IGovernanceCommandService { }
public sealed class IndicatorTargetService(IGovernanceExecutionStore s) : GovernanceCommandService(s, "indicator_target", "indicator.target.created"), IIndicatorTargetService
{ protected override void Validate(IReadOnlyDictionary<string, object?> v) { Required(v, "name", "Indicador"); Required(v, "source", "Fonte"); Required(v, "periodicity", "Periodicidade"); Required(v, "target_value", "Meta"); } }
public interface IIndicatorMeasurementService : IGovernanceCommandService { }
public sealed class IndicatorMeasurementService(IGovernanceExecutionStore s) : GovernanceCommandService(s, "indicator_measurement", "indicator.measurement.created"), IIndicatorMeasurementService
{ protected override void Validate(IReadOnlyDictionary<string, object?> v) { Required(v, "indicator_target_id", "Indicador"); Required(v, "source", "Fonte"); Required(v, "value", "Valor"); } }
public interface IIndicatorTrendService { string Calculate(decimal? previous, decimal? current); }
public sealed class IndicatorTrendService : IIndicatorTrendService
{ public string Calculate(decimal? previous, decimal? current) => previous is null || current is null ? "insufficient_data" : current > previous ? "up" : current < previous ? "down" : "stable"; }

public interface IExecutiveReportTemplateService : IGovernanceCommandService { }
public sealed class ExecutiveReportTemplateService(IGovernanceExecutionStore s) : GovernanceCommandService(s, "executive_report_template", "executive_report.template.created"), IExecutiveReportTemplateService
{ protected override void Validate(IReadOnlyDictionary<string, object?> v) { Required(v, "name", "Nome do template"); } }
public interface IExecutiveReportComposerService { void ValidateEvidence(IEnumerable<Guid> evidenceIds); }
public sealed class ExecutiveReportComposerService : IExecutiveReportComposerService
{ public void ValidateEvidence(IEnumerable<Guid> evidenceIds) { if (!evidenceIds.Any(id => id != Guid.Empty)) throw new ValidationException("O relatório executivo precisa citar evidências verificáveis."); } }
public interface IGovernanceReportService : IExecutiveReportComposerService { }
public sealed class GovernanceReportService : IGovernanceReportService
{ public void ValidateEvidence(IEnumerable<Guid> evidenceIds) { if (!evidenceIds.Any(id => id != Guid.Empty)) throw new ValidationException("O relatório de governança precisa citar evidências verificáveis."); } }
