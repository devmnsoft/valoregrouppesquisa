using System.Text.Json;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.GovernanceExecution;

namespace Valora.Infrastructure.Repositories;

/// <summary>Tenant-scoped write gateway for governance commands and their immutable audit trail.</summary>
public sealed class GovernanceExecutionStore(IDbConnectionFactory factory) : IGovernanceExecutionStore
{
    public async Task ExecuteAsync(GovernanceCommand command, CancellationToken cancellationToken)
    {
        var values = command.Values;
        object? Value(string key) => values.TryGetValue(key, out var value) ? value : null;
        var parameters = new
        {
            command.EntityId, command.OrganizationId, command.ActorUserId,
            objective = Value("objective"), periodStart = Value("period_start"), periodEnd = Value("period_end"),
            title = Value("title"), scheduledAt = Value("scheduled_at"), context = Value("context"),
            responsible = Value("responsible_user_id"), status = Value("status"), justification = Value("justification"),
            decisionId = Value("decision_id"), evidenceId = Value("evidence_id"), sessionId = Value("session_id"),
            actionItemId = Value("action_item_id"), evidence = Value("evidence_summary"), metric = Value("metric_name"),
            value = Value("value"), source = Value("source"), learning = Value("learning"), name = Value("name"),
            periodicity = Value("periodicity"), targetValue = Value("target_value"),
            payload = JsonSerializer.Serialize(values), command.EventType, command.EntityType
        };

        var sql = command.EntityType switch
        {
            "governance_cycle" => "INSERT INTO valorapesquisa.governance_cycles(id,organization_id,objective,period_start,period_end,created_by_user_id) VALUES(@EntityId,@OrganizationId,@objective::text,@periodStart::date,@periodEnd::date,@ActorUserId)",
            "governance_meeting" => "INSERT INTO valorapesquisa.governance_meetings(id,organization_id,title,scheduled_at,created_by_user_id) VALUES(@EntityId,@OrganizationId,@title::text,@scheduledAt::timestamptz,@ActorUserId)",
            "governance_decision" => "INSERT INTO valorapesquisa.governance_decisions(id,organization_id,title,context,justification,responsible_user_id,status,created_by_user_id) VALUES(@EntityId,@OrganizationId,@title::text,@context::text,@justification::text,@responsible::uuid,coalesce(@status::text,'draft'),@ActorUserId)",
            "decision_evidence_link" => "INSERT INTO valorapesquisa.decision_evidence_links(decision_id,evidence_id,evidence_type,rationale,linked_by_user_id) VALUES(@decisionId::uuid,@evidenceId::uuid,'intelligence',coalesce(@evidence::text,'Vínculo explícito'),@ActorUserId)",
            "evolution_milestone" => "INSERT INTO valorapesquisa.evolution_milestones(id,organization_id,evolution_cycle_id,title,description,evidence_summary,occurred_at,reached_at) VALUES(@EntityId,@OrganizationId,(@payload::jsonb->>'evolution_cycle_id')::uuid,@title::text,coalesce(@payload::jsonb->>'description',''),@evidence::text,now(),now())",
            "evolution_metric_snapshot" => "INSERT INTO valorapesquisa.evolution_metric_snapshots(id,organization_id,evolution_cycle_id,metric_name,value,source,measured_at) VALUES(@EntityId,@OrganizationId,(@payload::jsonb->>'evolution_cycle_id')::uuid,@metric::text,@value::numeric,@source::text,now())",
            "organizational_learning_event" => "INSERT INTO valorapesquisa.organizational_learning_events(id,organization_id,event_type,learning,evidence_summary,source_type,created_by_user_id) VALUES(@EntityId,@OrganizationId,'retrospective',@learning::text,@evidence::text,'governance',@ActorUserId)",
            "one_on_one_action_link" => "INSERT INTO valorapesquisa.one_on_one_action_links(session_id,action_item_id,linked_by_user_id) VALUES(@sessionId::uuid,@actionItemId::uuid,@ActorUserId)",
            "indicator_target" => "INSERT INTO valorapesquisa.indicator_targets(id,organization_id,name,target_value,unit,source,periodicity,owner_user_id) VALUES(@EntityId,@OrganizationId,@name::text,@targetValue::numeric,coalesce(@payload::jsonb->>'unit','unidade'),@source::text,@periodicity::text,@ActorUserId)",
            "indicator_measurement" => "INSERT INTO valorapesquisa.indicator_measurements(id,organization_id,indicator_target_id,value,source,measured_at,measured_by_user_id) VALUES(@EntityId,@OrganizationId,(@payload::jsonb->>'indicator_target_id')::uuid,@value::numeric,@source::text,now(),@ActorUserId)",
            "executive_report_template" => "INSERT INTO valorapesquisa.executive_report_templates(id,organization_id,name,report_type,created_by_user_id) VALUES(@EntityId,@OrganizationId,@name::text,coalesce(@payload::jsonb->>'report_type','governance'),@ActorUserId)",
            _ => throw new InvalidOperationException("Tipo de registro de governança não suportado.")
        };

        using var connection = factory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO valorapesquisa.audit_logs(organization_id,user_id,action,entity_type,entity_id,message,metadata_json,module) VALUES(@OrganizationId,@ActorUserId,@EventType,@EntityType,@EntityId::text,'Operação de governança registrada',@payload::jsonb,'governance')",
            parameters, transaction, cancellationToken: cancellationToken));
        transaction.Commit();
    }
}
