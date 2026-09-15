using Valora.Application.ActionCenter;
using Valora.Application.Workspace;
using System.ComponentModel.DataAnnotations;
using Valora.Tests.Support;

namespace Valora.Tests;

public sealed class ActionCenterPagingTests
{
    [Theory]
    [InlineData("in_progress", "Em execução")]
    [InlineData("blocked", "Bloqueada")]
    [InlineData("completed", "Concluída")]
    [InlineData("critical", "Crítica")]
    public void Action_labels_are_shared_between_views(string code, string expected)
    {
        Assert.Equal(expected, ActionStatuses.Label(code));
    }

    [Theory]
    [InlineData(-4, 500, 1, 50)]
    [InlineData(2, 25, 2, 25)]
    public void Item_query_normalizes_invalid_page_parameters(int page, int size, int expectedPage, int expectedSize)
    {
        var query = new ActionItemListQuery(Page: page, PageSize: size);

        Assert.Equal(expectedPage, query.ValidPage);
        Assert.Equal(expectedSize, query.ValidPageSize);
    }

    [Theory]
    [InlineData(0, 1, 1, false, false)]
    [InlineData(21, 1, 3, false, true)]
    [InlineData(21, 2, 3, true, true)]
    [InlineData(21, 3, 3, true, false)]
    [InlineData(21, 999, 3, true, false)]
    public void Page_result_exposes_accessible_navigation(int total, int page, int pages, bool previous, bool next)
    {
        var result = new PageResult<int>(Array.Empty<int>(), page, 10, total);

        Assert.Equal(pages, result.TotalPages);
        Assert.Equal(previous, result.HasPrevious);
        Assert.Equal(next, result.HasNext);
    }
}

public sealed class ActionPlanClosurePolicyTests
{
    [Theory]
    [InlineData("in_execution", 0, true)]
    [InlineData("in_execution", 1, false)]
    [InlineData("completed", 0, false)]
    [InlineData("canceled", 0, false)]
    [InlineData("approved", 0, false)]
    public void Completion_requires_execution_state_and_no_pending_conditions(string status, int pending, bool expected)
    {
        Assert.Equal(expected, ActionPlanClosurePolicy.CanComplete(status, pending));
    }

    [Fact]
    public void Typed_confirmation_is_enforced_by_server_validation()
    {
        var request = new CompleteActionPlanRequest { Result = "Resultado válido", Evidence = "Evidência", Version = 7, CommandId = "intent-1", Confirmed = false };
        var errors = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        Assert.Contains(errors, error => error.ErrorMessage == "Confirme a revisão do resultado e das evidências.");
    }
}

public sealed class ActionPlanClosureExperienceTests
{
    [Fact]
    public void Review_view_avoids_page_directive_collision_and_preserves_navigation_context()
    {
        var view = File.ReadAllText(RepositoryPaths.WebFile("Views", "ActionCenter", "ReviewClosure.cshtml"));

        Assert.Contains("var activitiesPage = Model.Activities", view);
        Assert.DoesNotContain("var page = Model.Activities", view);
        Assert.DoesNotContain("@page.", view);
        Assert.Contains("name=\"activityPage\" value=\"@(activitiesPage.CurrentPage)\"", view);
        Assert.Contains("asp-route-activityStatus=\"@Model.ActivityStatus\"", view);
        Assert.Contains("asp-route-returnUrl=\"@Model.ReturnUrl\"", view);
    }

    [Fact]
    public void Closure_script_isolates_drafts_and_never_persists_confirmation()
    {
        var script = File.ReadAllText(RepositoryPaths.WebFile("wwwroot", "js", "action-plan-closure.js"));

        Assert.Contains("page.dataset.draftKey", script);
        Assert.Contains("textarea[name=\"Command.Result\"]", script);
        Assert.Contains("input[type=\"checkbox\"][name=\"Command.Confirmed\"]", script);
        Assert.Contains("confirmation.checked = false", script);
        Assert.DoesNotContain("Command.Confirmed':", script);
        Assert.Contains("storage.remove(marker.dataset.closureClearKey)", script);
    }
}

public sealed class ActionCenterFilterContractTests
{
    [Fact]
    public void Dashboard_filters_are_typed_and_do_not_require_fake_identifiers()
    {
        var unassigned = new ActionItemListQuery(Assignment: ActionAssignmentFilter.Unassigned, Scope: ActionItemScopeFilter.Open);
        var active = new ActionPlanListQuery(Scope: ActionPlanScopeFilter.Active);
        Assert.Equal(ActionAssignmentFilter.Unassigned, unassigned.Assignment);
        Assert.Null(unassigned.ResponsibleUserId);
        Assert.Equal(ActionPlanScopeFilter.Active, active.Scope);
    }

    [Theory]
    [InlineData("2026-09-13", "2026-09-14", true)]
    [InlineData("2026-09-14", "2026-09-14", false)]
    [InlineData("2026-09-15", "2026-09-14", false)]
    public void Civil_due_date_only_becomes_overdue_after_its_local_day(string due, string today, bool overdue)
    {
        Assert.Equal(overdue, DateOnly.Parse(due) < DateOnly.Parse(today));
    }
}
