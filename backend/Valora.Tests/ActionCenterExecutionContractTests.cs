namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class ActionCenterExecutionContractTests {
    private static readonly string Items = File.ReadAllText(Support.RepositoryPaths.InfrastructureFile("Repositories", "ActionItemRepository.cs"));
    private static readonly string Plans = File.ReadAllText(Support.RepositoryPaths.InfrastructureFile("Repositories", "ActionPlanRepository.cs"));
    private static readonly string Contracts = File.ReadAllText(Support.RepositoryPaths.ApplicationFile("ActionCenter", "ActionCenterContracts.cs"));
    private static readonly string Schema = File.ReadAllText(Support.RepositoryPaths.CanonicalDatabaseScript);

    [Fact] public void Transition_locks_authorized_item_and_records_real_previous_state_atomically() { Assert.Contains("FOR UPDATE OF i",Items); Assert.Contains("from=current.Status",Items); Assert.Contains("affected != 1",Items); Assert.Contains("await z.CommitAsync()",Items); }
    [Fact] public void Progress_reserves_one_hundred_for_evidenced_completion() { Assert.Contains("Range(0,99)",Contracts); Assert.Contains("new[]{\"pending\",\"in_progress\",\"overdue\"}",Items); Assert.Contains("\"completed\",100",Items); }
    [Fact] public void Version_and_command_key_protect_concurrency_and_replays() { Assert.Contains("i.organization_id=@o",Items); Assert.Contains("current.Version != version",Items); Assert.Contains("h.command_id=@commandId",Items); Assert.Contains("pg_advisory_xact_lock",Items); Assert.Contains("replay.IntentHash==fingerprint",Items); Assert.Contains("ux_action_item_history_command",Schema); }
    [Fact] public void Resume_is_explicit_and_preserves_progress() { Assert.Contains("\"resume\",new[]{\"blocked\"},\"in_progress\",null",Items); Assert.DoesNotContain("progress_percent=0",Items); Assert.Contains("CanResume",Contracts); }
    [Fact] public void Every_materialized_action_projection_contains_the_extended_contract() { foreach(var field in new[]{"Version","ResponsibleName","PlanTitle","CompletionResult"}) Assert.Contains(field,Items); foreach(var field in new[]{"OwnerName","ActiveItemCount"}) Assert.Contains(field,Plans); }
    [Fact] public void Dashboard_aggregates_before_limiting_recent_rows() { var aggregate=Plans.IndexOf("count(*) FILTER",StringComparison.Ordinal); var limit=Plans.IndexOf("LIMIT 12",StringComparison.Ordinal); Assert.True(aggregate>=0&&limit>aggregate); }
    [Fact] public void Plan_progress_excludes_canceled_activities() { Assert.Contains("i.status<>'canceled'",Plans); }
    [Fact] public void Direct_creation_is_authorized_idempotent_and_does_not_use_title_as_identity() {
        Assert.Contains("action_item_create_commands",Items); Assert.Contains("pg_advisory_xact_lock",Items);
        Assert.Contains("p.owner_user_id IS NULL OR p.owner_user_id=@u",Items);
        Assert.Contains("organization_modules",Items); Assert.Contains("CreateFingerprint",Items);
        Assert.DoesNotContain("lower(btrim(existing.title))",Items);
    }
    [Fact] public void Operational_changes_are_versioned_idempotent_and_atomic() { foreach(var operation in new[]{"Edit","Assign","Reschedule"}) Assert.Contains($"Task {operation}",Items); Assert.Contains("from_value",Items); Assert.Contains("to_value",Items); Assert.Contains("action_plan_change_history",Plans); Assert.Contains("version=version+1",Plans); Assert.Contains("action_operational_management",Schema); }
    [Fact] public void Dashboard_agenda_repeats_module_scope_and_navigation_accepts_the_center() { Assert.True(Plans.Split("om_scope.module_code='organizational_intelligence'").Length>=4); var controller=File.ReadAllText(Support.RepositoryPaths.WebFile("Controllers", "ActionPlansController.cs")); Assert.Contains("\"/ActionCenter\",\"/ActionCenter/Items\"",controller); }
    [Fact] public void Plan_creation_is_tenant_validated_and_idempotent() { Assert.Contains("action_plan_create_commands",Plans); Assert.Contains("create-action-plan",Plans); Assert.Contains("organization_modules",Plans); Assert.Contains("organizational_governance_cycles",Plans); Assert.DoesNotContain("lower(btrim(existing.title))",Plans); Assert.Contains("action_plan_create_commands",Schema); }
    [Fact] public void Dashboard_and_listing_share_organization_civil_day_semantics() { Assert.Contains("i.due_at AT TIME ZONE org.time_zone",Plans); Assert.Contains("i.due_at AT TIME ZONE org.time_zone",Items); Assert.DoesNotContain("i.due_at<now()",Plans); }
    [Fact] public void Plan_listing_is_counted_filtered_and_stably_paginated_in_postgresql() {
        Assert.Contains("Task<PageResult<ActionPlanDto>> List",Plans); Assert.Contains("SELECT count(*)::int",Plans);
        Assert.Contains("LIMIT @size OFFSET @offset",Plans); Assert.Contains("p.created_at DESC,p.id DESC",Plans);
        Assert.Contains("AT TIME ZONE",Plans);
    }

    [Fact] public void Edit_forms_preserve_every_priority_and_use_consistent_plan_binding() {
        var plan=File.ReadAllText(Support.RepositoryPaths.WebFile("Views", "ActionCenter", "EditPlan.cshtml"));
        var item=File.ReadAllText(Support.RepositoryPaths.WebFile("Views", "ActionCenter", "ItemDetails.cshtml"));
        foreach(var priority in new[]{"critical","high","medium","low"}) { Assert.Contains($"value=\"{priority}\"",plan); Assert.Contains($"==\"{priority}\"",item); }
        Assert.Contains("asp-for=\"Command.Title\"",plan); Assert.Contains("asp-validation-for=\"Command.Title\"",plan);
        Assert.Contains("Bind(Prefix=\"Command\")",File.ReadAllText(Support.RepositoryPaths.WebFile("Controllers", "ActionPlansController.cs")));
    }
    [Fact] public void Failed_dialogs_preserve_original_intent_including_unassignment() {
        var plan=File.ReadAllText(Support.RepositoryPaths.WebFile("Views", "ActionCenter", "PlanDetails.cshtml"));
        var item=File.ReadAllText(Support.RepositoryPaths.WebFile("Views", "ActionCenter", "ItemDetails.cshtml"));
        Assert.Contains("assignCommand is null?plan.OwnerUserId:assignCommand.ResponsibleUserId",plan);
        Assert.Contains("assignCommand?.CommandId??Guid.NewGuid()",plan);
        Assert.Contains("dueCommand?.Version??plan.Version",plan);
        Assert.Contains("assignCommand is null?i.ResponsibleUserId:assignCommand.ResponsibleUserId",item);
        Assert.True(plan.Split("asp-validation-summary=\"All\"").Length>=4);
    }
    [Fact] public void Plan_history_is_tenant_scoped_filtered_and_stably_paginated() {
        Assert.Contains("Task<PageResult<ActionPlanHistoryDto>> History",Plans);
        Assert.Contains("p.organization_id=@o",Plans); Assert.Contains("h.operation=@filter",Plans);
        Assert.Contains("ORDER BY h.changed_at DESC,h.id DESC LIMIT @size OFFSET @offset",Plans);
        Assert.Contains("AT TIME ZONE org.time_zone)::date DueAt",Plans);
        Assert.Contains("AT TIME ZONE org.time_zone)::date DueAt",Items);
    }
}
