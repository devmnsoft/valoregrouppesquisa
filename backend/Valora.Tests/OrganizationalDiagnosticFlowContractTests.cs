using Valora.Application.Access;
using Valora.Application.OrganizationalIntelligence;

namespace Valora.Tests;

public sealed class OrganizationalDiagnosticFlowContractTests
{
    [Fact]
    public void CompleteWorkflowPermissionsAreCanonical()
    {
        string[] permissions =
        [
            "diagnostics.read", "diagnostics.manage", "forms.read", "forms.manage",
            "responses.read", "responses.submit", "results.read", "results.manage",
            "intelligence.read", "intelligence.process", "dashboard.read", "heatmap.read",
            "benchmark.read", "action.read", "action.manage", "evolution.read", "journey.read",
            "reports.read", "reports.generate", "certificates.read", "certificates.generate",
            "certificates.validate", "administration.read", "administration.manage"
        ];

        Assert.All(permissions, permission => Assert.True(
            ValoraAccessCatalog.IsCanonicalPermission(permission),
            $"Missing canonical workflow permission: {permission}"));
    }

    [Fact]
    public void IntelligenceJobAcceptsMissingDatabaseScheduleTimestamps()
    {
        var job = new IntelligenceProcessingJob(
            Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, "diagnosis_closed",
            IntelligenceProcessingStatus.Pending, 0, 0, 3, null, null, null, null,
            null, null, null, null, "correlation-id", DateTime.UtcNow);

        Assert.Null(job.ScheduledAt);
        Assert.Null(job.StartedAt);
        Assert.Null(job.CompletedAt);
        Assert.Null(job.FailedAt);
    }
}
