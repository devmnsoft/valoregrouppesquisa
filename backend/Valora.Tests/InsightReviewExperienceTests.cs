using Valora.Tests.Support;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class InsightReviewExperienceTests {
    [Fact]
    public void InsightReviewUsesRealProtectedEndpointsAndConfirmation() {
        var controller = File.ReadAllText(RepositoryPaths.WebFile("Controllers", "InsightsController.cs"));
        var view = File.ReadAllText(RepositoryPaths.WebFile("Views", "Insights", "Details.cshtml"));

        Assert.Contains("ValidateAntiForgeryToken", controller);
        Assert.Contains("ApproveAiInsightUseCase", controller);
        Assert.Contains("RejectAiInsightUseCase", controller);
        Assert.Contains("ILogger<InsightsController>", controller);
        Assert.Contains("HttpContext.TraceIdentifier", controller);
        Assert.Contains("asp-action=\"Approve\"", view);
        Assert.Contains("asp-action=\"Reject\"", view);
        Assert.Contains("data-confirm=", view);
        Assert.Contains("data-dialog-target=\"#rejectInsightDialog\"", view);
        Assert.Contains("expectedVersion", view);
        Assert.Contains("commandKey", view);
    }

    [Fact]
    public void InsightActionsNavigateToExistingBusinessFlows() {
        var view = File.ReadAllText(RepositoryPaths.WebFile("Views", "Insights", "Details.cshtml"));
        var actionPlan = File.ReadAllText(RepositoryPaths.WebFile("Views", "ActionCenter", "CreatePlan.cshtml"));

        Assert.Contains("asp-controller=\"ActionCenter\"", view);
        Assert.Contains("asp-action=\"CreatePlan\"", view);
        Assert.Contains("asp-route-originType=\"ai_insight\"", view);
        Assert.Contains("asp-route-originId=\"@x.Id\"", view);
        Assert.Contains("href=\"/Decisions\"", view);
        Assert.Contains("href=\"/Reports\"", view);
        Assert.Contains("value=\"ai_insight\"", actionPlan);
        Assert.Contains("AntiForgeryToken", actionPlan);
    }

    [Fact]
    public void Database_contract_keeps_review_and_recipient_delivery_independent() {
        var sql = File.ReadAllText(RepositoryPaths.BackendFile("database", "postgresql", "script_completo.sql"));

        Assert.Contains("review_version bigint NOT NULL DEFAULT 0", sql);
        Assert.Contains("valora_ai_review_commands", sql);
        Assert.Contains("organization_id,user_id,related_entity_id,type", sql);
        Assert.DoesNotContain("DELETE FROM valorapesquisa.notifications", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Review_endpoints_use_distinct_canonical_permissions_and_tab_scoped_drafts() {
        var controller = File.ReadAllText(RepositoryPaths.WebFile("Controllers", "InsightsController.cs"));
        var view = File.ReadAllText(RepositoryPaths.WebFile("Views", "Insights", "Details.cshtml"));

        Assert.Contains("Policy = ValoraPermissions.Insights.Read", controller);
        Assert.Contains("Policy = ValoraPermissions.Insights.Approve", controller);
        Assert.Contains("Policy = ValoraPermissions.Insights.Reject", controller);
        Assert.Contains("sessionStorage", view);
        Assert.Contains("x.OrganizationId", view);
        Assert.Contains("var scope=Convert.ToHexString", view);
        Assert.Contains("Model.ReviewVersion", view);
        Assert.DoesNotContain("PendingRejectionReason", controller);
    }

    [Fact]
    public void Plan_creation_links_the_reviewed_insight_in_the_same_transaction() {
        var repository = File.ReadAllText(RepositoryPaths.BackendFile("Valora.Infrastructure", "Repositories", "ActionPlanRepository.cs"));
        var insightRepository = File.ReadAllText(RepositoryPaths.BackendFile("Valora.Infrastructure", "Repositories", "ValoraAiHubRepositories.cs"));

        Assert.Contains("action_plan_create_commands", repository);
        Assert.Contains("status='converted_to_action'", repository);
        Assert.Contains("LinkedPlanId", insightRepository);
        Assert.Contains("pg_advisory_xact_lock", insightRepository);
        Assert.DoesNotContain("SetStatusAsync", insightRepository);
    }
}
