namespace Valora.Application.ValoraAi;

public sealed class EvidencePackBuilderService(IValoraAiEvidenceRepository evidence) : IEvidencePackBuilderService {
    public Task<AiEvidencePack> BuildAsync(AiRunContext context, CancellationToken ct) => evidence.BuildAsync(context, ct);
}

public sealed class AiGuardrailValidationService(IValoraAiGuardrailService guardrails) : IAiGuardrailValidationService {
    public ValoraAiValidation Validate(string output, ValoraEvidencePack evidence) => guardrails.Validate(output, evidence);
}

public sealed class InsightGenerationService(IValoraAiOrchestrator orchestrator) : IInsightGenerationService {
    public Task<ValoraAiExecutionResult> GenerateAsync(AiRunContext context, AiEvidencePack evidence, CancellationToken ct) {
        var source = new ValoraEvidenceSource(context.OrganizationId, "Organização", context.DiagnosticId,
            "Valora", context.MethodologyVersionId?.ToString() ?? "vigente", true,
            evidence.Items.Select(x => new ValoraEvidence(x.Id.ToString(), x.Type, x.Summary, x.Dimension, x.IndexCode)).ToArray(),
            new Dictionary<string, decimal>(), evidence.Items.Select(x => x.Dimension).OfType<string>().Distinct().ToArray(),
            [], [], [], [], [], evidence.Limitations);
        return orchestrator.ExecuteAsync(context.OrganizationId, context.DiagnosticId,
            ValoraOfficialPrompts.All.Single(x => x.Code == "insights"), source, context.CorrelationId, ct);
    }
}

public sealed class ValoraAiRunService(IValoraAiRunRepository runs) {
    public Task<AiUsageAllowance> CheckAllowanceAsync(Guid organizationId, CancellationToken ct) => runs.CheckAllowanceAsync(organizationId, ct);
}

public sealed class AiFeedbackService(IValoraAiFeedbackRepository feedback) : IAiFeedbackService {
    public Task RecordRejectionAsync(AiReviewCommand command, Guid runId, CancellationToken ct) {
        if (string.IsNullOrWhiteSpace(command.Reason)) throw new ArgumentException("O motivo da rejeição é obrigatório.", nameof(command));
        return feedback.RecordAsync(command.OrganizationId, command.InsightId, runId, command.ReviewerId, "rejection", command.Reason.Trim(), ct);
    }
}

public sealed class AiReviewService(IValoraAiReviewRepository reviews) {
    public async Task ReviewAsync(AiReviewCommand command, CancellationToken ct) {
        if (command.Decision is not (AiInsightStatuses.Approved or AiInsightStatuses.Rejected)) throw new ArgumentException("Decisão de revisão inválida.");
        if (string.IsNullOrWhiteSpace(command.CommandKey) || command.CommandKey.Length > 160)
            throw new ArgumentException("A chave idempotente da revisão é obrigatória e deve ter até 160 caracteres.");
        if (command.Decision == AiInsightStatuses.Rejected && string.IsNullOrWhiteSpace(command.Reason))
            throw new ArgumentException("O motivo da rejeição é obrigatório.");
        var result = await reviews.ApplyAsync(command with { CommandKey = command.CommandKey ?? Guid.NewGuid().ToString("N") }, ct);
        if (result == AiReviewResult.NotFound) throw new KeyNotFoundException("Insight não encontrado.");
        if (result == AiReviewResult.Conflict)
            throw new Valora.Application.Exceptions.ConcurrencyConflictException("O insight foi alterado por outra pessoa ou esta chave já foi usada com outro conteúdo. Atualize os dados; sua justificativa pode ser reenviada.");
    }
}

public sealed class ValoraAiOrchestratorService(IEvidencePackBuilderService packs, IInsightGenerationService generation, IValoraAiEvidenceRepository evidence) {
    public async Task<ValoraAiExecutionResult> GenerateAsync(AiRunContext context, CancellationToken ct) {
        var pack = await packs.BuildAsync(context, ct);
        if (pack.Items.Count == 0)
            return new(new ValoraAiRun(Guid.NewGuid(), context.OrganizationId, context.DiagnosticId, "insights", 1, "none", "none", AiRunStatus.Invalid, context.CorrelationId, DateTime.UtcNow, "insufficient_evidence"), null, null, AiInsufficientEvidence.Message);
        var result = await generation.GenerateAsync(context, pack, ct);
        await evidence.SaveAsync(pack, result.Run.Id, ct);
        return result;
    }
}

public sealed class GenerateInsightsFromResultUseCase(ValoraAiOrchestratorService orchestrator) { public Task<ValoraAiExecutionResult> ExecuteAsync(AiRunContext context, CancellationToken ct) => orchestrator.GenerateAsync(context, ct); }
public sealed class BuildEvidencePackUseCase(IEvidencePackBuilderService packs) { public Task<AiEvidencePack> ExecuteAsync(AiRunContext context, CancellationToken ct) => packs.BuildAsync(context, ct); }
public sealed class ReviewAiInsightUseCase(AiReviewService reviews) { public Task ExecuteAsync(AiReviewCommand command, CancellationToken ct) => reviews.ReviewAsync(command, ct); }
public sealed class ApproveAiInsightUseCase(AiReviewService reviews) { public Task ExecuteAsync(Guid o, Guid i, Guid u, string commandKey, long expectedVersion, CancellationToken ct) => reviews.ReviewAsync(new(o, i, u, AiInsightStatuses.Approved, null, commandKey, expectedVersion), ct); }
public sealed class RejectAiInsightUseCase(AiReviewService reviews) { public Task ExecuteAsync(Guid o, Guid i, Guid u, string reason, string commandKey, long expectedVersion, CancellationToken ct) => reviews.ReviewAsync(new(o, i, u, AiInsightStatuses.Rejected, reason, commandKey, expectedVersion), ct); }
public sealed class ConvertInsightToActionUseCase(IValoraAiInsightRepository insights) { public Task ExecuteAsync(Guid o, Guid i, Guid u, CancellationToken ct) => insights.SetStatusAsync(o, i, AiInsightStatuses.ConvertedToAction, u, ct); }
public sealed class ConvertInsightToDecisionUseCase(IValoraAiInsightRepository insights) { public Task ExecuteAsync(Guid o, Guid i, Guid u, CancellationToken ct) => insights.SetStatusAsync(o, i, AiInsightStatuses.ConvertedToDecision, u, ct); }
public sealed class GenerateAiExecutiveSummaryUseCase(IValoraAiOrchestrator orchestrator) { public Task<ValoraAiExecutionResult> ExecuteAsync(Guid o, Guid d, ValoraEvidenceSource e, string c, CancellationToken ct) => orchestrator.ExecuteAsync(o, d, ValoraOfficialPrompts.All.Single(x => x.Code == "executive_reading"), e, c, ct); }
