using System.Text.Json;
using Valora.Application.Results;

namespace Valora.Application.OrganizationalIntelligence;

public sealed class OrganizationalIntelligenceService(IOrganizationalIntelligenceRepository repository) : IOrganizationalIntelligenceService
{
    public Task<OrganizationalIntelligenceDashboardDto> DashboardAsync(Guid organizationId, CancellationToken ct) => repository.GetDashboardAsync(organizationId, ct);
    public Task<IReadOnlyList<OrganizationalIntelligenceRunDto>> RunsAsync(Guid organizationId, CancellationToken ct) => repository.ListRunsAsync(organizationId, ct);
    public Task<OrganizationalIntelligenceRunDto?> RunAsync(Guid organizationId, Guid id, CancellationToken ct) => repository.GetRunAsync(organizationId, id, ct);
    public Task<IReadOnlyList<OrganizationalJourneyEventDto>> JourneyAsync(Guid organizationId, CancellationToken ct) => repository.ListJourneyAsync(organizationId, ct);
    public Task<IReadOnlyList<ValoraIndicatorDefinitionDto>> IndicatorsAsync(CancellationToken ct) => repository.ListIndicatorsAsync(ct);
    public Task<IReadOnlyList<ValoraActionDto>> ActionsAsync(Guid organizationId, CancellationToken ct) => repository.ListActionsAsync(organizationId, ct);
    public Task<IReadOnlyList<ValoraActionHistoryDto>> ActionHistoryAsync(Guid organizationId, Guid actionId, CancellationToken ct) => repository.ListActionHistoryAsync(organizationId, actionId, ct);
    public Task<bool> DeleteActionAsync(Guid organizationId, Guid actionId, Guid userId, CancellationToken ct) => repository.DeleteActionAsync(organizationId, actionId, userId, ct);
    public Task<IReadOnlyList<EvidenceItemDto>> EvidenceItemsAsync(Guid organizationId, CancellationToken ct) => repository.ListEvidenceItemsAsync(organizationId, ct);
    public Task<IReadOnlyList<IntelligenceModuleRecordDto>> ModuleRecordsAsync(Guid organizationId, string module, CancellationToken ct) => repository.ListModuleRecordsAsync(organizationId, module, ct);

    public async Task<ValoraActionDto?> UpdateActionAsync(Guid organizationId, Guid actionId, Guid userId, UpdateValoraActionRequest request, CancellationToken ct)
    {
        string[] statuses = ["recommended", "planned", "in_progress", "waiting", "overdue", "completed", "cancelled", "reviewed", "replanned"];
        if (!statuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Status inválido para o plano de ação.");

        var current = (await repository.ListActionsAsync(organizationId, ct)).FirstOrDefault(action => action.Id == actionId);
        if (current is null) return null;
        var status = request.Status.ToLowerInvariant();
        var owner = request.Owner ?? current.Owner;
        var dueAt = request.DueAt ?? current.DueAt;
        if (status is "planned" or "in_progress" or "replanned")
        {
            if (string.IsNullOrWhiteSpace(owner) || dueAt is null || string.IsNullOrWhiteSpace(current.CompletionCriteria))
                throw new ArgumentException("Uma ação planejada precisa de responsável, prazo e critério de conclusão.");
        }
        if (status == "completed" && string.IsNullOrWhiteSpace(request.Notes))
            throw new ArgumentException("Registre a aprendizagem antes de concluir esta ação.");
        if (status == "cancelled" && string.IsNullOrWhiteSpace(request.Notes))
            throw new ArgumentException("Informe a justificativa para cancelar a ação.");
        if (status == "replanned" && string.IsNullOrWhiteSpace(request.Notes))
            throw new ArgumentException("Informe a justificativa para replanejar a ação.");

        return await repository.UpdateActionAsync(organizationId, actionId, request with { Status = status }, userId, ct);
    }

    public async Task<IReadOnlyList<EvolutionPointDto>> EvolutionAsync(Guid organizationId, CancellationToken ct)
    {
        var runs = (await repository.ListRunsAsync(organizationId, ct)).OrderBy(x => x.CreatedAt).ToList();
        return runs.Select((run, index) =>
        {
            var change = index == 0 ? 0 : Math.Round(run.MaturityIndex - runs[index - 1].MaturityIndex, 2);
            var classification = index == 0 ? "baseline" : change >= 2 ? "evolution" : change <= -2 ? "regression" : Math.Abs(change) < .5m ? "stagnation" : "stable";
            var sufficient = index >= 2;
            decimal? estimate = sufficient ? Math.Clamp(Math.Round(run.MaturityIndex + (run.MaturityIndex - runs[index - 2].MaturityIndex) / 2, 2), 0, 100) : null;
            return new EvolutionPointDto(run.CreatedAt, run.MaturityIndex, change, classification, sufficient, estimate);
        }).ToList();
    }

    public async Task<ValoraActionDto> CreateActionAsync(Guid organizationId, Guid userId, CreateValoraActionRequest request, CancellationToken ct)
    {
        if (string.Equals(request.SourceType, "insight", StringComparison.OrdinalIgnoreCase))
        {
            if (request.InsightId is null)
                throw new ArgumentException("Informe o Insight que sustenta a ação.");

            var insight = (await repository.ListModuleRecordsAsync(organizationId, "insights", ct))
                .FirstOrDefault(item => item.Id == request.InsightId.Value);
            if (insight is null)
                throw new ArgumentException("O Insight informado não existe no escopo desta organização.");
            if (!HasTraceableEvidence(insight.Data))
                throw new ArgumentException("O Insight ainda não possui evidências rastreáveis para ser transformado em Action.");

            request = request with
            {
                InferenceId = ReadGuid(insight.Data, "inferenceId") ?? request.InferenceId,
                ConceptCode = ReadText(insight.Data, "concept") ?? request.ConceptCode,
                EvidenceJustification = BuildEvidenceJustification(insight.Data, request.EvidenceJustification)
            };
        }
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.EvidenceJustification) || string.IsNullOrWhiteSpace(request.CompletionCriteria))
            throw new ArgumentException("Título, justificativa baseada em evidências e critério de conclusão são obrigatórios.");
        if (request.EvidenceJustification.Trim().Length < 20)
            throw new ArgumentException("Descreva as evidências que sustentam a ação (mínimo de 20 caracteres).");
        if (string.IsNullOrWhiteSpace(request.Capability) || string.IsNullOrWhiteSpace(request.Indicators) || string.IsNullOrWhiteSpace(request.ExpectedResult))
            throw new ArgumentException("Capacidade organizacional, indicador e resultado esperado são obrigatórios.");
        if (!new[] { "critical", "high", "medium", "low" }.Contains(request.Priority, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Prioridade inválida para o plano de ação.");
        if (!new[] { "low", "medium", "high" }.Contains(request.Complexity, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Complexidade inválida para o plano de ação.");
        var now = DateTime.UtcNow;
        var item = new ValoraActionDto(Guid.NewGuid(), organizationId, $"ACT-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            request.Title.Trim(), request.Description.Trim(), request.EvidenceJustification.Trim(), request.Capability.Trim(),
            request.Priority.Trim().ToLowerInvariant(), request.Owner?.Trim(), request.ExecutiveSponsor?.Trim(), request.DueAt,
            request.Complexity.Trim().ToLowerInvariant(), request.Indicators.Trim(), request.ExpectedResult.Trim(), request.CompletionCriteria.Trim(), "recommended", now, now,
            request.SurveyId, request.CycleId, request.InsightId, request.InferenceId, request.ConceptCode?.Trim(), request.Urgency?.Trim(), request.Impact?.Trim());
        return await repository.CreateActionAsync(item, userId, ct);
    }

    private static bool HasTraceableEvidence(JsonElement data) =>
        data.TryGetProperty("evidenceIds", out var evidence) && evidence.ValueKind == JsonValueKind.Array && evidence.GetArrayLength() > 0;

    private static string BuildEvidenceJustification(JsonElement data, string supplied)
    {
        var count = data.GetProperty("evidenceIds").GetArrayLength();
        var summary = ReadText(data, "evidenceSummary") ?? $"Insight sustentado por {count} evidência(s) rastreável(is).";
        return string.IsNullOrWhiteSpace(supplied) ? summary : $"{summary} {supplied.Trim()}";
    }

    private static string? ReadText(JsonElement data, string property) =>
        data.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static Guid? ReadGuid(JsonElement data, string property) =>
        Guid.TryParse(ReadText(data, property), out var value) ? value : null;

    public async Task<OrganizationalIntelligenceRunDto> GenerateAsync(Guid organizationId, CancellationToken ct)
    {
        var evidence = await repository.GetEvidenceAsync(organizationId, ct);
        var ordered = evidence.Dimensions.OrderByDescending(x => x.Score).ToList();
        var maturity = ordered.Count == 0 ? 0 : Math.Round(ordered.Average(x => x.Score), 2);
        var culture = AverageMatching(ordered, "cultur", "confian", "pessoas", "lider");
        var governance = AverageMatching(ordered, "governan", "execu", "process", "estrat");
        if (culture == 0) culture = maturity;
        if (governance == 0) governance = maturity;
        var gap = ordered.Count < 2 ? 0 : Math.Round(ordered.First().Score - ordered.Last().Score, 2);
        var confidence = EvidenceConfidence.Classify(evidence.Total) switch
        {
            "muito alta" => "very_high",
            "alta" => "high",
            "moderada" => "moderate",
            _ => "low"
        };
        const string warning = ValoraInsightDevolutivaService.InsufficientEvidenceMessage;
        var runId = Guid.NewGuid();
        // An insight is a recommendation, not merely a formatted score.  Do not persist
        // recommendations until the aggregate contains the minimum evidence required by
        // the method.  The run is still persisted so the UI can honestly explain why a
        // reading was not produced and the attempt remains auditable.
        var insights = evidence.Total < 3
            ? new List<OrganizationalInsightDto>()
            : ordered.Where(dimension => dimension.EvidenceCount >= 3).TakeLast(Math.Min(3, ordered.Count)).Select((dimension, index) =>
        {
            return new OrganizationalInsightDto(
            Guid.NewGuid(), runId, dimension.Name,
            $"A dimensão {dimension.Name} apresenta índice consolidado de {dimension.Score:0.#}%.",
            $"Leitura agregada de {dimension.EvidenceCount} avaliações válidas; nenhuma resposta individual é exposta.",
            "A posição relativa foi calculada somente entre dimensões observadas no mesmo conjunto de evidências.",
            "Hipótese a validar: práticas e rotinas associadas à dimensão podem não estar consistentes.",
            "Hipótese de impacto: pode reduzir a consistência da execução; requer validação pela organização.",
            index == 0 ? "high" : "medium",
            "Validar a observação com os responsáveis, definir uma ação mensurável e reavaliar no próximo ciclo.", DateTime.UtcNow);
        }).ToList();
        var run = new OrganizationalIntelligenceRunDto(runId, organizationId, maturity, culture, governance, gap,
            ordered.FirstOrDefault()?.Name ?? "Sem evidências", ordered.LastOrDefault()?.Name ?? "Sem evidências",
            evidence.Total, confidence, evidence.Total < 3 ? warning : null, ordered, insights, DateTime.UtcNow);
        await repository.SaveAnalysisAsync(run, ct);
        return run;
    }

    public Task<OrganizationalJourneyEventDto> CreateJourneyAsync(Guid organizationId, Guid userId, CreateJourneyEventRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
            throw new ArgumentException("Título e descrição são obrigatórios.");
        var item = new OrganizationalJourneyEventDto(Guid.NewGuid(), organizationId, request.Title.Trim(), request.Description.Trim(),
            string.IsNullOrWhiteSpace(request.EventType) ? "milestone" : request.EventType.Trim().ToLowerInvariant(), request.OccurredAt ?? DateTime.UtcNow, userId, DateTime.UtcNow);
        return repository.CreateJourneyEventAsync(item, ct);
    }

    private static decimal AverageMatching(IEnumerable<DimensionHeatmapDto> values, params string[] terms)
    {
        var selected = values.Where(x => terms.Any(t => (x.Code + " " + x.Name).Contains(t, StringComparison.OrdinalIgnoreCase))).ToList();
        return selected.Count == 0 ? 0 : Math.Round(selected.Average(x => x.Score), 2);
    }
}
