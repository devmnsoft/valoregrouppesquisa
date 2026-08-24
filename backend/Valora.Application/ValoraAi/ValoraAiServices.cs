using System.Text.Json;
using System.Text.RegularExpressions;

namespace Valora.Application.ValoraAi;

public sealed class ValoraEvidencePackBuilder : IValoraEvidencePackBuilder
{
    public ValoraEvidencePack Build(ValoraEvidenceSource source)
    {
        var minimized = source.Evidence
            .Where(e => !string.IsNullOrWhiteSpace(e.Id) && !string.IsNullOrWhiteSpace(e.Summary))
            .Select(e => source.Anonymous ? e with { PersonName = null, PersonEmail = null } : e)
            .ToArray();
        return new(source.OrganizationId, source.Organization, source.DiagnosisId, source.Methodology,
            source.MethodologyVersion, source.Anonymous, minimized, source.Scores, source.Dimensions,
            source.Concepts, source.ConsolidatedAnswers, source.History, source.PreviousActionPlans,
            source.PreviousReports, source.PrivacyLimits);
    }
}

public sealed class ValoraPromptRenderer : IValoraPromptRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public ValoraRenderedPrompt Render(ValoraPromptTemplate template, ValoraEvidencePack pack)
    {
        var evidenceJson = JsonSerializer.Serialize(pack, JsonOptions);
        var user = template.UserTemplate.Replace("{{evidence_pack}}", evidenceJson, StringComparison.Ordinal);
        return new(template.Code, template.Version, template.SystemInstructions, user, template.OutputSchema);
    }
}

public sealed class ValoraAiGuardrailService : IValoraAiGuardrailService
{
    private static readonly Regex Email = new(@"\b[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled);
    public ValoraAiValidation Validate(string output, ValoraEvidencePack pack)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(output)) return new(false, ["empty_output"]);
        JsonDocument document;
        try { document = JsonDocument.Parse(output); }
        catch (JsonException) { return new(false, ["invalid_json"]); }
        using (document)
        {
            var items = document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.EnumerateArray().ToArray()
                : document.RootElement.TryGetProperty("items", out var list) && list.ValueKind == JsonValueKind.Array
                    ? list.EnumerateArray().ToArray() : [document.RootElement];
            if (items.Length == 0) errors.Add("empty_output");
            var allowed = pack.Evidence.Select(e => e.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                if (!item.TryGetProperty("evidence_ids", out var ids) || ids.ValueKind != JsonValueKind.Array || ids.GetArrayLength() == 0)
                    errors.Add("missing_evidence_ids");
                else if (ids.EnumerateArray().Any(id => id.ValueKind != JsonValueKind.String || !allowed.Contains(id.GetString()!)))
                    errors.Add("unknown_or_invented_evidence");
                var recommendation = Text(item, "recommendation");
                if (!string.IsNullOrWhiteSpace(recommendation) && string.IsNullOrWhiteSpace(Text(item, "probable_cause")) && string.IsNullOrWhiteSpace(Text(item, "associated_risk")))
                    errors.Add("recommendation_without_cause_or_risk");
                if (!string.IsNullOrWhiteSpace(Text(item, "priority")) && string.IsNullOrWhiteSpace(Text(item, "priority_justification")))
                    errors.Add("priority_without_justification");
            }
        }
        if (pack.Anonymous && Email.IsMatch(output)) errors.Add("personal_data_in_anonymous_diagnosis");
        return new(errors.Count == 0, errors.Distinct().ToArray());
    }
    private static string? Text(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}

public sealed class ValoraAiReviewService(IValoraAiRunRepository runs) : IValoraAiReviewService
{
    public Task ReviewAsync(Guid runId, Guid reviewerId, AiRunStatus current, AiRunStatus target, string? note, CancellationToken ct)
    {
        var allowed = (current, target) is
            (AiRunStatus.Draft, AiRunStatus.PendingReview) or
            (AiRunStatus.PendingReview, AiRunStatus.Approved) or
            (AiRunStatus.PendingReview, AiRunStatus.Rejected) or
            (AiRunStatus.Approved, AiRunStatus.Published) or
            (AiRunStatus.Rejected, AiRunStatus.Draft);
        if (!allowed) throw new InvalidOperationException($"Transição de revisão inválida: {current} -> {target}.");
        return runs.RecordReviewAsync(runId, reviewerId, target, note, ct);
    }
}

public sealed class ValoraAiOrchestrator(IValoraAiProvider provider, IValoraPromptRenderer renderer,
    IValoraEvidencePackBuilder evidenceBuilder, IValoraAiGuardrailService guardrails,
    IValoraAiRunRepository runs) : IValoraAiOrchestrator
{
    public async Task<ValoraAiExecutionResult> ExecuteAsync(Guid organizationId, Guid diagnosisId,
        ValoraPromptTemplate prompt, ValoraEvidenceSource evidence, string correlationId, CancellationToken ct)
    {
        var pack = evidenceBuilder.Build(evidence);
        var allowance = await runs.CheckAllowanceAsync(organizationId, ct);
        var status = !allowance.Allowed ? AiRunStatus.LimitExceeded : !provider.IsConfigured ? AiRunStatus.NotConfigured : AiRunStatus.Draft;
        var run = new ValoraAiRun(Guid.NewGuid(), organizationId, diagnosisId, prompt.Code, prompt.Version,
            provider.IsConfigured ? "configured" : "none", "none", status, correlationId, DateTime.UtcNow);
        await runs.CreateAsync(run, pack, ct);
        if (!allowance.Allowed) return await Stop(run, "Limite mensal de IA atingido.", "usage_limit", ct);
        if (!provider.IsConfigured) return await Stop(run, "IA não configurada", "provider_not_configured", ct);
        if (pack.Evidence.Count == 0) return await Stop(run with { Status = AiRunStatus.Invalid }, "Insuficiência de dados para conclusão.", "insufficient_evidence", ct);
        try
        {
            var result = await provider.CompleteAsync(new("default", renderer.Render(prompt, pack), correlationId), ct);
            var validation = guardrails.Validate(result.Content, pack);
            var finalStatus = validation.IsValid ? AiRunStatus.PendingReview : AiRunStatus.Invalid;
            await runs.CompleteAsync(run.Id, finalStatus, result.Content, validation, result, validation.IsValid ? null : string.Join(',', validation.Violations), ct);
            return new(run with { Status = finalStatus, Provider = result.Provider, Model = result.Model }, result.Content, validation,
                validation.IsValid ? "Execução aguardando revisão humana." : "Saída bloqueada pelos guardrails.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await runs.CompleteAsync(run.Id, AiRunStatus.Failed, null, null, null, ex.Message, ct);
            return new(run with { Status = AiRunStatus.Failed, Error = ex.Message }, null, null, "Provider indisponível; nenhum entregável foi alterado.");
        }
    }
    private async Task<ValoraAiExecutionResult> Stop(ValoraAiRun run, string message, string error, CancellationToken ct)
    {
        await runs.CompleteAsync(run.Id, run.Status, null, null, null, error, ct);
        return new(run with { Error = error }, null, null, message);
    }
}

public static class ValoraOfficialPrompts
{
    public const string MandatoryInstructions = """
        A IA do Valora não é chatbot: interpreta organizações. Nunca invente dados ou estatísticas; nunca conclua sem evidência; nunca trate sintomas como causas; nunca use frases motivacionais, julgamento moral ou culpa pessoal. Explique observação, evidência, correlação, causa provável, impacto, prioridade e plano de evolução. Se a base for insuficiente, declare insuficiência de dados. Responda somente JSON conforme o schema.
        """;
    private static readonly string[] Definitions = ["executive_reading|Leitura executiva", "insights|Geração de insights",
        "risks|Análise de riscos", "probable_causes|Causas prováveis", "recommendations|Recomendações",
        "action_plan|Plano de ação", "executive_report|Relatório executivo", "dashboard_summary|Resumo para dashboard",
        "dimension_interpretation|Interpretação por dimensão", "historical_evolution|Evolução histórica"];
    public static IReadOnlyList<ValoraPromptTemplate> All { get; } = Definitions.Select(value =>
    {
        var parts = value.Split('|'); var now = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc);
        return new ValoraPromptTemplate(parts[0], parts[1], 1, $"Produzir {parts[1].ToLowerInvariant()} baseada em evidências.",
            MandatoryInstructions, "Analise exclusivamente este evidence pack minimizado: {{evidence_pack}}",
            "{\"type\":\"array\",\"items\":{\"required\":[\"title\",\"interpretation\",\"evidence_ids\",\"impact\",\"priority\",\"priority_justification\",\"confidence\",\"analysis_limitations\"]}}",
            "active", now, now);
    }).ToArray();
}

public sealed class DisabledValoraAiProvider : IValoraAiProvider
{
    public bool IsConfigured => false;
    public Task<ValoraAiProviderResult> CompleteAsync(ValoraAiRequest request, CancellationToken ct) =>
        throw new InvalidOperationException("IA não configurada");
}
