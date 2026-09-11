using Xunit;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class PriorityActionContractTests {
    private static readonly string Repository = File.ReadAllText(
        Support.RepositoryPaths.InfrastructureFile("Repositories", "PriorityActionRepository.cs"));

    [Fact]
    public void Priority_read_uses_uuid_model_real_schema_and_keeps_row_lock() {
        Assert.Contains("QuerySingleOrDefaultAsync<PriorityAccess>", Repository);
        Assert.Contains("FOR UPDATE", Repository);
        Assert.DoesNotContain("executive_priorities WHERE id=@priority AND organization_id=@o AND deleted_at", Repository);
        Assert.DoesNotContain("ExecuteScalarAsync<int>(new CommandDefinition(VisiblePriority", Repository);
        Assert.Contains("Guid Id, Guid OrganizationId, string Status", Repository);
    }

    [Fact]
    public void Idempotency_fingerprint_and_persisted_context_include_actor_priority_and_operation() {
        Assert.Contains("Organization = organization, Priority = priority, Operation = operation, Actor = actor", Repository);
        Assert.Contains("priority_id PriorityId,operation Operation", Repository);
        Assert.Contains("previous.PriorityId != priority || previous.ActorId != u || previous.Operation != operation", Repository);
        Assert.Contains("(organization_id,priority_id,command_id,operation,request_hash,result_id,created_by_user_id)", Repository);
    }

    [Fact]
    public void Linked_resources_and_responsible_are_authorized_inside_the_transaction() {
        Assert.Contains("FOR UPDATE OF i", Repository);
        Assert.Contains("EnsureAccessible(action, u, wide", Repository);
        Assert.Contains("EnsureEligibleResponsible(unit, o", Repository);
        Assert.Contains("await unit.CommitAsync()", Repository);
        Assert.Contains("RecordJourney(unit", Repository);
    }
}
