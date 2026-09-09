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
    }

    [Fact]
    public void InsightActionsNavigateToExistingBusinessFlows() {
        var view = File.ReadAllText(RepositoryPaths.WebFile("Views", "Insights", "Details.cshtml"));
        var actionPlan = File.ReadAllText(RepositoryPaths.WebFile("Views", "ActionCenter", "CreatePlan.cshtml"));

        Assert.Contains("/ActionCenter/Plans/Create?originType=ai_insight", view);
        Assert.Contains("href=\"/Decisions\"", view);
        Assert.Contains("href=\"/Reports\"", view);
        Assert.Contains("value=\"ai_insight\"", actionPlan);
        Assert.Contains("AntiForgeryToken", actionPlan);
    }
}
