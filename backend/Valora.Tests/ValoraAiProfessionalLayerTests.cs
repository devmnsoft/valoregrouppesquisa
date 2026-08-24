using Valora.Application.ValoraAi;

namespace Valora.Tests;

public sealed class ValoraAiProfessionalLayerTests
{
    private static ValoraEvidenceSource Source(bool anonymous = true, params ValoraEvidence[] evidence) => new(
        Guid.NewGuid(), "Organização", Guid.NewGuid(), "Valora", "1.0", anonymous, evidence,
        new Dictionary<string, decimal> { ["cultura"] = 72 }, ["Cultura"], ["Confiança"], ["Consolidado"], [], [], [], ["LGPD"]);

    [Fact]
    public void Prompt_renders_versioned_evidence_pack()
    {
        var pack = new ValoraEvidencePackBuilder().Build(Source(true, new("ev-1", "score", "Score consolidado")));
        var rendered = new ValoraPromptRenderer().Render(ValoraOfficialPrompts.All[0], pack);
        Assert.Contains("ev-1", rendered.User); Assert.DoesNotContain("{{evidence_pack}}", rendered.User); Assert.Equal(1, rendered.Version);
    }

    [Fact]
    public void Anonymous_pack_removes_personal_data()
    {
        var pack = new ValoraEvidencePackBuilder().Build(Source(true, new("ev-1", "answer", "Consolidado", PersonName: "Ana", PersonEmail: "ana@example.com")));
        Assert.Null(pack.Evidence[0].PersonName); Assert.Null(pack.Evidence[0].PersonEmail);
    }

    [Fact]
    public void Valid_json_with_known_evidence_is_accepted()
    {
        var pack = new ValoraEvidencePackBuilder().Build(Source(true, new("ev-1", "score", "Convergência")));
        const string json = """[{"title":"Risco","interpretation":"Leitura","evidence_ids":["ev-1"],"impact":"alto","priority":"alta","priority_justification":"impacto","confidence":"high","analysis_limitations":"amostra","recommendation":"monitorar","probable_cause":"processo"}]""";
        Assert.True(new ValoraAiGuardrailService().Validate(json, pack).IsValid);
    }

    [Theory]
    [InlineData("{}", "missing_evidence_ids")]
    [InlineData("not-json", "invalid_json")]
    [InlineData("[{\"evidence_ids\":[\"inventada\"]}]", "unknown_or_invented_evidence")]
    public void Invalid_or_unsupported_output_is_rejected(string json, string violation)
    {
        var pack = new ValoraEvidencePackBuilder().Build(Source(true, new("ev-1", "score", "Real")));
        var result = new ValoraAiGuardrailService().Validate(json, pack);
        Assert.False(result.IsValid); Assert.Contains(violation, result.Violations);
    }

    [Fact]
    public async Task Missing_provider_is_controlled_and_does_not_call_it()
    {
        var repository = new MemoryRuns();
        var service = new ValoraAiOrchestrator(new DisabledValoraAiProvider(), new ValoraPromptRenderer(),
            new ValoraEvidencePackBuilder(), new ValoraAiGuardrailService(), repository);
        var result = await service.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), ValoraOfficialPrompts.All[0],
            Source(true, new("ev-1", "score", "Real")), "corr", default);
        Assert.Equal(AiRunStatus.NotConfigured, result.Run.Status); Assert.Equal("IA não configurada", result.Message);
    }

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
