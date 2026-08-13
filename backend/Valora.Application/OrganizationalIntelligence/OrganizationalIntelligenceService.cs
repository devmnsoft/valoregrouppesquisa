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

    public Task<ValoraActionDto?> UpdateActionAsync(Guid organizationId, Guid actionId, Guid userId, UpdateValoraActionRequest request, CancellationToken ct)
    {
        string[] statuses = ["recommended", "planned", "in_progress", "waiting", "completed", "cancelled", "reviewed"];
        if (!statuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Status inválido para o plano de ação.");
        return repository.UpdateActionAsync(organizationId, actionId, request with { Status = request.Status.ToLowerInvariant() }, userId, ct);
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

    public Task<ValoraActionDto> CreateActionAsync(Guid organizationId, Guid userId, CreateValoraActionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.EvidenceJustification) || string.IsNullOrWhiteSpace(request.CompletionCriteria))
            throw new ArgumentException("Título, justificativa baseada em evidências e critério de conclusão são obrigatórios.");
        if (request.EvidenceJustification.Trim().Length < 20)
            throw new ArgumentException("Descreva as evidências que sustentam a ação (mínimo de 20 caracteres).");
        var now = DateTime.UtcNow;
        var item = new ValoraActionDto(Guid.NewGuid(), organizationId, $"ACT-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            request.Title.Trim(), request.Description.Trim(), request.EvidenceJustification.Trim(), request.Capability.Trim(),
            request.Priority.Trim().ToLowerInvariant(), request.Owner?.Trim(), request.ExecutiveSponsor?.Trim(), request.DueAt,
            request.Complexity.Trim().ToLowerInvariant(), request.Indicators.Trim(), request.ExpectedResult.Trim(), request.CompletionCriteria.Trim(), "recommended", now, now);
        return repository.CreateActionAsync(item, userId, ct);
    }

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
