using Valora.Application.ValoraAi;

namespace Valora.Tests;

public sealed class ValoraAiProfessionalLayerTests
{
    private static ValoraEvidenceSource Source(bool anonymous = true, params ValoraEvidence[] evidence) => new(
        Guid.NewGuid(), "Organização", Guid.NewGuid(), "Valora", "1.0", anonymous, evidence,
        new Dictionary<string, decimal> { ["cultura"] = 72 }, ["Cultura"], ["Confiança"], ["Consolidado"], [], [], [], ["LGPD"]);
 
    [Fact]
    public async Task Plan_limit_blocks_execution_gracefully()
    {
        var repository = new MemoryRuns(false);
        var service = new ValoraAiOrchestrator(new DisabledValoraAiProvider(), new ValoraPromptRenderer(),
            new ValoraEvidencePackBuilder(), new ValoraAiGuardrailService(), repository);
        var result = await service.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), ValoraOfficialPrompts.All[0], Source(), "corr", default);
        Assert.Equal(AiRunStatus.LimitExceeded, result.Run.Status);
    }

    [Fact]
    public async Task Human_review_approves_or_rejects_only_pending_insights()
    {
        var repository = new MemoryRuns();
        var review = new ValoraAiReviewService(repository);
        await review.ReviewAsync(Guid.NewGuid(), Guid.NewGuid(), AiRunStatus.PendingReview, AiRunStatus.Approved, "validado", default);
        await review.ReviewAsync(Guid.NewGuid(), Guid.NewGuid(), AiRunStatus.PendingReview, AiRunStatus.Rejected, "revisar", default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => review.ReviewAsync(Guid.NewGuid(), Guid.NewGuid(), AiRunStatus.Draft, AiRunStatus.Published, null, default));
    }

    [Fact]
    public void Official_catalog_has_all_ten_versioned_prompts_and_ai_permissions()
    {
        Assert.Equal(10, ValoraOfficialPrompts.All.Count);
        Assert.All(ValoraOfficialPrompts.All, prompt => Assert.Contains("Nunca invente", prompt.SystemInstructions));
        Assert.Contains("ai.insights.review", Valora.Application.Access.ValoraPermissions.All);
    }

    private sealed class MemoryRuns(bool allowed = true) : IValoraAiRunRepository
    {
        public Task CreateAsync(ValoraAiRun run, ValoraEvidencePack input, CancellationToken ct) => Task.CompletedTask;
        public Task CompleteAsync(Guid runId, AiRunStatus status, string? output, ValoraAiValidation? validation, ValoraAiProviderResult? usage, string? error, CancellationToken ct) => Task.CompletedTask;
        public Task<AiUsageAllowance> CheckAllowanceAsync(Guid organizationId, CancellationToken ct) => Task.FromResult(new AiUsageAllowance(allowed, allowed ? 0 : 10, 10));
        public Task RecordReviewAsync(Guid runId, Guid reviewerId, AiRunStatus status, string? note, CancellationToken ct) => Task.CompletedTask;
    }
}
