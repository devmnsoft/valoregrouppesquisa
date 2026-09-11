using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Valora.Application.ActionCenter;
using Valora.Application.Contracts;
using Valora.Application.Exceptions;

namespace Valora.Infrastructure.Repositories;

public sealed class PriorityActionRepository(IDbConnectionFactory db, IDbTransactionFactory transactions) : IPriorityActionRepository {
    // executive_priorities intentionally has no deleted_at column. The row lock is part of the
    // command contract: two requests for the same priority must not validate stale state.
    private const string VisiblePriority = """
        SELECT id Id, organization_id OrganizationId, status Status,
               owner_user_id OwnerUserId, created_by CreatedBy
        FROM valorapesquisa.executive_priorities
        WHERE id=@priority AND organization_id=@o
          AND (owner_user_id IS NULL OR owner_user_id=@u OR @wide)
        """;

    public async Task<IReadOnlyList<PriorityActionDto>> List(Guid o, Guid priority, Guid u, bool wide, CancellationToken ct) {
        ValidateContext(o, u, priority);
        using var connection = db.Create();
        const string sql = """
            SELECT a.id ActionId,a.action_plan_id PlanId,p.title PlanTitle,a.title,a.status,a.priority,
                   a.responsible_user_id ResponsibleUserId,a.due_at DueAt,a.progress_percent ProgressPercent,
                   a.evidence_summary EvidenceSummary,a.expected_outcome ExpectedOutcome,
                   a.completion_evidence CompletionEvidence,l.created_at LinkedAt
            FROM valorapesquisa.priority_action_links l
            JOIN valorapesquisa.action_items a ON a.id=l.action_item_id AND a.organization_id=l.organization_id AND a.deleted_at IS NULL
            JOIN valorapesquisa.action_plans p ON p.id=a.action_plan_id AND p.organization_id=l.organization_id AND p.deleted_at IS NULL
            WHERE l.organization_id=@o AND l.priority_id=@priority AND l.deleted_at IS NULL
              AND EXISTS(SELECT 1 FROM valorapesquisa.executive_priorities ep
                         WHERE ep.id=@priority AND ep.organization_id=@o
                           AND (ep.owner_user_id IS NULL OR ep.owner_user_id=@u OR @wide))
              AND (@wide OR a.responsible_user_id IS NULL OR a.responsible_user_id=@u OR p.owner_user_id=@u)
            ORDER BY l.created_at,a.id
            """;
        return (await connection.QueryAsync<PriorityActionDto>(new CommandDefinition(sql, new { o, priority, u, wide }, cancellationToken: ct))).AsList();
    }

    public Task<Guid> Link(Guid o, Guid u, Guid priority, LinkPriorityActionRequest request, bool wide, CancellationToken ct) =>
        Execute(o, u, priority, request.CommandId, "link", new { request.ActionId }, wide, async unit => {
            var action = await unit.Connection.QuerySingleOrDefaultAsync<ResourceAccess>(new CommandDefinition("""
                SELECT i.id Id,i.status Status,i.responsible_user_id ResponsibleUserId,p.owner_user_id PlanOwnerUserId
                FROM valorapesquisa.action_items i
                JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id AND p.deleted_at IS NULL
                WHERE i.id=@ActionId AND i.organization_id=@o AND i.deleted_at IS NULL
                FOR UPDATE OF i
                """, new { o, request.ActionId }, unit.Transaction, cancellationToken: ct));
            EnsureAccessible(action, u, wide, "Atividade");
            EnsureCanReceiveLink(action.Status, "Atividade");
            await InsertLink(unit, o, u, priority, request.ActionId, ct);
            return request.ActionId;
        }, ct);

    public Task<Guid> Create(Guid o, Guid u, Guid priority, CreatePriorityActionRequest request, bool wide, CancellationToken ct) {
        var normalized = new NormalizedCreate(request.PlanId, request.CreatePlan, request.PlanTitle?.Trim(),
            request.Title?.Trim() ?? "", request.Description?.Trim() ?? "", request.ExpectedOutcome?.Trim() ?? "",
            request.EvidenceSummary?.Trim() ?? "", request.Priority?.Trim().ToLowerInvariant() ?? "", request.ResponsibleUserId, request.DueAt);
        return Execute(o, u, priority, request.CommandId, "create", normalized, wide, async unit => {
            await EnsureEligibleResponsible(unit, o, normalized.ResponsibleUserId, ct);
            var plan = normalized.PlanId;
            if (normalized.CreatePlan) {
                plan = Guid.NewGuid();
                await unit.Connection.ExecuteAsync(new CommandDefinition("""
                    INSERT INTO valorapesquisa.action_plans
                        (id,organization_id,title,summary,origin_type,origin_id,status,priority,owner_user_id,
                         created_by_user_id,due_at,evidence_summary,expected_outcome)
                    VALUES (@plan,@o,@PlanTitle,@Description,'priority',@priority,'draft',@Priority,
                            @ResponsibleUserId,@u,@DueAt,@EvidenceSummary,@ExpectedOutcome)
                    """, new { plan, o, u, priority, normalized.PlanTitle, Description=normalized.Description, Priority=normalized.Priority, normalized.ResponsibleUserId, normalized.DueAt, normalized.EvidenceSummary, normalized.ExpectedOutcome }, unit.Transaction, cancellationToken: ct));
            }
            else {
                var existingPlan = await unit.Connection.QuerySingleOrDefaultAsync<ResourceAccess>(new CommandDefinition("""
                    SELECT id Id,status Status,owner_user_id ResponsibleUserId,NULL::uuid PlanOwnerUserId
                    FROM valorapesquisa.action_plans
                    WHERE id=@plan AND organization_id=@o AND deleted_at IS NULL
                    FOR UPDATE
                    """, new { o, plan }, unit.Transaction, cancellationToken: ct));
                EnsureAccessible(existingPlan, u, wide, "Plano");
                EnsureCanReceiveActivity(existingPlan.Status);
            }

            var action = Guid.NewGuid();
            await unit.Connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO valorapesquisa.action_items
                    (id,organization_id,action_plan_id,title,description,origin_type,origin_id,priority,status,
                     responsible_user_id,due_at,evidence_summary,expected_outcome)
                VALUES (@action,@o,@plan,@Title,@Description,'priority',@priority,@Priority,'pending',
                        @ResponsibleUserId,@DueAt,@EvidenceSummary,@ExpectedOutcome)
                """, new { action, o, plan, priority, normalized.Title, normalized.Description, normalized.Priority, normalized.ResponsibleUserId, normalized.DueAt, normalized.EvidenceSummary, normalized.ExpectedOutcome }, unit.Transaction, cancellationToken: ct));
            await InsertLink(unit, o, u, priority, action, ct);
            await RecordJourney(unit, o, u, action, normalized.Title, normalized.EvidenceSummary, ct);
            return action;
        }, ct);
    }

    private async Task<Guid> Execute(Guid o, Guid u, Guid priority, string command, string operation, object payload, bool wide, Func<IUnitOfWork, Task<Guid>> work, CancellationToken ct) {
        ValidateContext(o, u, priority);
        if (string.IsNullOrWhiteSpace(command)) throw new ArgumentException("Informe a chave da operação.", nameof(command));
        command = command.Trim();
        var hash = Fingerprint(o, priority, operation, u, payload);
        await using var unit = await transactions.BeginAsync(ct);
        await unit.Connection.ExecuteAsync(new CommandDefinition("SELECT pg_advisory_xact_lock(hashtextextended(@lockKey,0))", new { lockKey = $"{o:N}:{command}" }, unit.Transaction, cancellationToken: ct));

        var visible = await unit.Connection.QuerySingleOrDefaultAsync<PriorityAccess>(new CommandDefinition(VisiblePriority + " FOR UPDATE", new { o, u, priority, wide }, unit.Transaction, cancellationToken: ct));
        if (visible is null) throw new KeyNotFoundException("Prioridade não encontrada ou sem acesso.");
        if (!string.Equals(visible.Status, "active", StringComparison.Ordinal)) throw new InvalidOperationException("Somente prioridades ativas podem receber atividades.");

        var previous = await unit.Connection.QuerySingleOrDefaultAsync<PreviousCommand>(new CommandDefinition("""
            SELECT priority_id PriorityId,operation Operation,request_hash Hash,result_id ResultId,
                   created_by_user_id ActorId
            FROM valorapesquisa.priority_action_commands
            WHERE organization_id=@o AND command_id=@command
            """, new { o, command }, unit.Transaction, cancellationToken: ct));
        if (previous is not null) {
            if (previous.PriorityId != priority || previous.ActorId != u || previous.Operation != operation || previous.Hash != hash)
                throw new ConcurrencyConflictException("A chave da operação já foi usada em outro contexto.");
            var currentResult = await unit.Connection.QuerySingleOrDefaultAsync<ResourceAccess>(new CommandDefinition("""
                SELECT i.id Id,i.status Status,i.responsible_user_id ResponsibleUserId,p.owner_user_id PlanOwnerUserId
                FROM valorapesquisa.action_items i JOIN valorapesquisa.action_plans p ON p.id=i.action_plan_id AND p.organization_id=i.organization_id AND p.deleted_at IS NULL
                WHERE i.id=@result AND i.organization_id=@o AND i.deleted_at IS NULL
                """, new { o, result=previous.ResultId }, unit.Transaction, cancellationToken:ct));
            EnsureAccessible(currentResult,u,wide,"Resultado da operação");
            return previous.ResultId;
        }

        var result = await work(unit);
        await unit.Connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO valorapesquisa.priority_action_commands
                (organization_id,priority_id,command_id,operation,request_hash,result_id,created_by_user_id)
            VALUES (@o,@priority,@command,@operation,@hash,@result,@u)
            """, new { o, priority, u, command, operation, hash, result }, unit.Transaction, cancellationToken: ct));
        await unit.CommitAsync();
        return result;
    }

    private static string Fingerprint(Guid organization, Guid priority, string operation, Guid actor, object payload) {
        var normalized = JsonSerializer.Serialize(new { Organization = organization, Priority = priority, Operation = operation, Actor = actor, Payload = payload }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private static void ValidateContext(Guid organization, Guid user, Guid priority) {
        if (organization == Guid.Empty) throw new ArgumentException("Organização inválida.", nameof(organization));
        if (user == Guid.Empty) throw new ArgumentException("Usuário inválido.", nameof(user));
        if (priority == Guid.Empty) throw new ArgumentException("Prioridade inválida.", nameof(priority));
    }

    private static void EnsureAccessible(ResourceAccess? resource, Guid user, bool wide, string name) {
        if (resource is null || resource.Status == "canceled" || (!wide && resource.ResponsibleUserId is not null && resource.ResponsibleUserId != user && resource.PlanOwnerUserId != user))
            throw new KeyNotFoundException($"{name} não encontrado, cancelado ou sem acesso.");
    }

    private static void EnsureCanReceiveLink(string status, string name) {
        if (status is not ("pending" or "in_progress" or "blocked" or "overdue"))
            throw new InvalidOperationException($"{name} no estado atual não pode receber um novo vínculo.");
    }

    private static void EnsureCanReceiveActivity(string status) {
        if (status is not ("draft" or "proposed" or "approved" or "in_execution"))
            throw new InvalidOperationException("O plano no estado atual não pode receber atividades.");
    }

    private static async Task EnsureEligibleResponsible(IUnitOfWork unit, Guid organization, Guid? responsible, CancellationToken ct) {
        if (responsible is null) return;
        if (responsible == Guid.Empty) throw new ArgumentException("Responsável inválido.", nameof(responsible));
        var eligible = await unit.Connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
            SELECT EXISTS(SELECT 1 FROM valorapesquisa.users
                          WHERE id=@responsible AND organization_id=@organization
                            AND deleted_at IS NULL AND status='active')
            """, new { organization, responsible }, unit.Transaction, cancellationToken: ct));
        if (!eligible) throw new ArgumentException("O responsável não está ativo nesta organização.", nameof(responsible));
    }

    private static Task InsertLink(IUnitOfWork unit, Guid o, Guid u, Guid priority, Guid action, CancellationToken ct) =>
        unit.Connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO valorapesquisa.priority_action_links(organization_id,priority_id,action_item_id,created_by_user_id)
            VALUES(@o,@priority,@action,@u)
            ON CONFLICT(organization_id,priority_id,action_item_id) WHERE deleted_at IS NULL DO NOTHING
            """, new { o, u, priority, action }, unit.Transaction, cancellationToken: ct));

    private static Task RecordJourney(IUnitOfWork unit, Guid o, Guid u, Guid action, string title, string evidence, CancellationToken ct) =>
        unit.Connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO valorapesquisa.journey_events
                (organization_id,event_type,title,description,source_type,source_id,impact_level,evidence_summary,occurred_at,created_by_user_id)
            VALUES(@o,'action_created',@title,'Atividade criada a partir de prioridade executiva.','action',@action,'medium',@evidence,now(),@u)
            """, new { o, u, action, title, evidence }, unit.Transaction, cancellationToken: ct));

    private sealed record NormalizedCreate(Guid? PlanId,bool CreatePlan,string? PlanTitle,string Title,string Description,string ExpectedOutcome,string EvidenceSummary,string Priority,Guid? ResponsibleUserId,DateTime? DueAt);
    private sealed record PriorityAccess(Guid Id, Guid OrganizationId, string Status, Guid? OwnerUserId, Guid CreatedBy);
    private sealed record ResourceAccess(Guid Id, string Status, Guid? ResponsibleUserId, Guid? PlanOwnerUserId);
    private sealed record PreviousCommand(Guid? PriorityId, string Operation, string Hash, Guid ResultId, Guid ActorId);
}
