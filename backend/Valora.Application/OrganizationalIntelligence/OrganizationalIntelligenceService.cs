namespace Valora.Application.OrganizationalIntelligence;

public sealed class OrganizationalIntelligenceService(IOrganizationalIntelligenceRepository repository) : IOrganizationalIntelligenceService
{
    public Task<OrganizationalIntelligenceDashboardDto> DashboardAsync(Guid organizationId, CancellationToken ct) => repository.GetDashboardAsync(organizationId, ct);
    public Task<IReadOnlyList<OrganizationalIntelligenceRunDto>> RunsAsync(Guid organizationId, CancellationToken ct) => repository.ListRunsAsync(organizationId, ct);
    public Task<OrganizationalIntelligenceRunDto?> RunAsync(Guid organizationId, Guid id, CancellationToken ct) => repository.GetRunAsync(organizationId, id, ct);
    public Task<IReadOnlyList<OrganizationalJourneyEventDto>> JourneyAsync(Guid organizationId, CancellationToken ct) => repository.ListJourneyAsync(organizationId, ct);
    public Task<IReadOnlyList<ValoraIndicatorDefinitionDto>> IndicatorsAsync(CancellationToken ct) => repository.ListIndicatorsAsync(ct);

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
        var confidence = evidence.Responses >= 30 && ordered.Count >= 4 ? "very_high" : evidence.Responses >= 15 && ordered.Count >= 3 ? "high" : evidence.Responses >= 5 && ordered.Count >= 2 ? "moderate" : "low";
        const string warning = "As informações disponíveis ainda não permitem concluir esta análise com segurança. Amplie a participação e gere uma nova leitura.";
        var runId = Guid.NewGuid();
        var insights = ordered.TakeLast(Math.Min(3, ordered.Count)).Select((dimension, index) =>
        {
            var convergent = dimension.EvidenceCount >= 3;
            return new OrganizationalInsightDto(
            Guid.NewGuid(), runId, dimension.Name,
            $"A dimensão {dimension.Name} apresenta índice consolidado de {dimension.Score:0.#}%.",
            $"Leitura agregada de {dimension.EvidenceCount} avaliações válidas; nenhuma resposta individual é exposta.",
            "A posição relativa foi calculada somente entre dimensões observadas no mesmo conjunto de evidências.",
            !convergent ? "Menos de 3 evidências convergentes: não é seguro formular uma causa provável." : "Hipótese a validar: práticas e rotinas associadas à dimensão podem não estar consistentes.",
            !convergent ? "Impacto não concluído por insuficiência de evidências convergentes." : "Hipótese de impacto: pode reduzir a consistência da execução; requer validação pela organização.",
            index == 0 ? "high" : "medium",
            "Validar a observação com os responsáveis, definir uma ação mensurável e reavaliar no próximo ciclo.", DateTime.UtcNow);
        }).ToList();
        var run = new OrganizationalIntelligenceRunDto(runId, organizationId, maturity, culture, governance, gap,
            ordered.FirstOrDefault()?.Name ?? "Sem evidências", ordered.LastOrDefault()?.Name ?? "Sem evidências",
            evidence.Total, confidence, confidence == "low" ? warning : null, ordered, insights, DateTime.UtcNow);
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
