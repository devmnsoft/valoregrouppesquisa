namespace Valora.Application.Indicators;

public sealed class IndicatorTrendService {
    public TrendResult Calculate(IReadOnlyList<IndicatorMeasurementDto> measurements, string comparisonRule = "higher_is_better") {
        return Calculate(measurements, comparisonRule, comparisonRule);
    }

    public TrendResult Calculate(IReadOnlyList<IndicatorMeasurementDto> measurements, string previousComparisonRule, string currentComparisonRule) {
        if (measurements.Count < 2) return new(IndicatorTrend.InsufficientData, measurements.Count, null, "Dados insuficientes: são necessárias ao menos duas medições verificáveis.");
        var ordered = measurements.OrderBy(x => x.MeasuredAt).ToArray();
        var delta = ordered[^1].Value - ordered[^2].Value;
        if (!string.Equals(previousComparisonRule, currentComparisonRule, StringComparison.Ordinal))
            return new(IndicatorTrend.ComparisonUnavailable, ordered.Length, delta,
                "Comparação indisponível: as duas medições pertencem a períodos com regras metodológicas diferentes.");
        if (currentComparisonRule is not ("higher_is_better" or "lower_is_better"))
            return new(IndicatorTrend.ComparisonUnavailable, ordered.Length, delta,
                $"Comparação indisponível: a regra '{currentComparisonRule}' exige parâmetros metodológicos que não foram definidos.");
        var adjusted = currentComparisonRule == "lower_is_better" ? -delta : delta;
        return new(adjusted > 0 ? IndicatorTrend.Improving : adjusted < 0 ? IndicatorTrend.Worsening : IndicatorTrend.Stable,
            ordered.Length, delta, "Tendência descritiva; não demonstra causalidade nem substitui decisão humana.");
    }
}

public sealed class IndicatorService(IIndicatorRepository repository) {
    public Task<IReadOnlyList<IndicatorDto>> List(Guid o, CancellationToken ct) => repository.List(RequireOrganization(o), ct);
    public Task<IndicatorDto?> Get(Guid o, Guid id, CancellationToken ct) => repository.Get(RequireOrganization(o), id, ct);
    public Task<Guid> Create(Guid o, CreateIndicatorRequest r, CancellationToken ct) {
        if (r.IsCalculated && string.IsNullOrWhiteSpace(r.Formula)) throw new ArgumentException("Indicador calculado exige fórmula ou regra clara.");
        return repository.Create(RequireOrganization(o), r, ct);
    }
    public Task Archive(Guid o, Guid id, CancellationToken ct) => repository.Archive(RequireOrganization(o), id, ct);
    internal static Guid RequireOrganization(Guid id) => id == Guid.Empty ? throw new InvalidOperationException("Selecione uma organização para acessar os indicadores.") : id;
}
public sealed class IndicatorTargetService(IIndicatorRepository repository) {
    public Task<IReadOnlyList<IndicatorTargetDto>> List(Guid o, Guid? id, CancellationToken ct) => repository.Targets(IndicatorService.RequireOrganization(o), id, ct);
    public Task<Guid> Create(Guid o, Guid id, CreateTargetRequest r, CancellationToken ct) {
        if (r.PeriodStart >= r.PeriodEnd) throw new ArgumentException("O fim do período deve ser posterior ao início.");
        return repository.CreateTarget(IndicatorService.RequireOrganization(o), id, r, ct);
    }
}
public sealed class IndicatorMeasurementService(IIndicatorRepository repository, IndicatorTrendService trends) {
    public Task<IReadOnlyList<IndicatorMeasurementDto>> List(Guid o, Guid id, CancellationToken ct) => repository.Measurements(IndicatorService.RequireOrganization(o), id, ct);
    public Task<Guid> Create(Guid o, Guid id, CreateMeasurementRequest r, CancellationToken ct) => repository.CreateMeasurement(IndicatorService.RequireOrganization(o), id, r, ct);
    public async Task<TrendResult> Trend(Guid o, Guid id, CancellationToken ct) {
        var measurements = await List(o, id, ct);
        if (measurements.Count < 2) return trends.Calculate(measurements);
        var ordered = measurements.OrderBy(x => x.MeasuredAt).ToArray();
        var targets = await repository.Targets(IndicatorService.RequireOrganization(o), id, ct);
        IndicatorTargetDto[] Applicable(DateTime measuredAt) => targets
            .Where(x => x.Status == "active" && x.PeriodStart <= measuredAt && x.PeriodEnd >= measuredAt)
            .ToArray();
        var previous = Applicable(ordered[^2].MeasuredAt);
        var current = Applicable(ordered[^1].MeasuredAt);
        if (previous.Length != 1 || current.Length != 1)
            return new(IndicatorTrend.ComparisonUnavailable, measurements.Count, null,
                previous.Length == 0 || current.Length == 0
                    ? "Comparação indisponível: não existe uma meta ativa para cada uma das duas medições analisadas."
                    : "Comparação indisponível: há mais de uma meta ativa aplicável a uma das medições analisadas.");
        return trends.Calculate(measurements, previous[0].ComparisonRule, current[0].ComparisonRule);
    }
}
public sealed class IndicatorAlertService(IIndicatorRepository repository) {
    public Task<IReadOnlyList<IndicatorAlertDto>> List(Guid o, CancellationToken ct) => repository.Alerts(IndicatorService.RequireOrganization(o), ct);
    public Task Resolve(Guid o, Guid id, Guid user, CancellationToken ct) => repository.ResolveAlert(IndicatorService.RequireOrganization(o), id, user, ct);
}
public sealed class ExecutiveScorecardService(IIndicatorRepository repository) {
    public Task<IReadOnlyList<ExecutiveScorecardDto>> List(Guid o, CancellationToken ct) => repository.Scorecards(IndicatorService.RequireOrganization(o), ct);
    public Task<Guid> Create(Guid o, Guid u, CreateScorecardRequest r, CancellationToken ct) => repository.CreateScorecard(IndicatorService.RequireOrganization(o), u, r, ct);
}
public sealed class AnalyticsSnapshotService(IIndicatorRepository repository) {
    public Task<IReadOnlyList<AnalyticsSnapshotDto>> List(Guid o, CancellationToken ct) => repository.Snapshots(IndicatorService.RequireOrganization(o), ct);
    public Task<Guid> Create(Guid o, Guid u, string name, CancellationToken ct) => repository.CreateSnapshot(IndicatorService.RequireOrganization(o), u, name, ct);
}
public sealed class IndicatorCategoryService(IIndicatorRepository repository) { public Task<IReadOnlyList<IndicatorDto>> List(Guid o, CancellationToken ct) => repository.List(IndicatorService.RequireOrganization(o), ct); }
public sealed class IndicatorSourceService(IIndicatorRepository repository) { public Task<IReadOnlyList<IndicatorDto>> List(Guid o, CancellationToken ct) => repository.List(IndicatorService.RequireOrganization(o), ct); }
public sealed class IndicatorFormulaService { }
public sealed class IndicatorAlertRuleService { }
