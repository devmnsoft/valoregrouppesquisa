namespace Valora.Application.Indicators;

public sealed class IndicatorTrendService
{
    public TrendResult Calculate(IReadOnlyList<IndicatorMeasurementDto> measurements, string comparisonRule = "higher_is_better")
    {
        if (measurements.Count < 2) return new(IndicatorTrend.InsufficientData, measurements.Count, null, "Dados insuficientes: são necessárias ao menos duas medições verificáveis.");
        var ordered = measurements.OrderBy(x => x.MeasuredAt).ToArray();
        var delta = ordered[^1].Value - ordered[^2].Value;
        var adjusted = comparisonRule == "lower_is_better" ? -delta : delta;
        return new(adjusted > 0 ? IndicatorTrend.Improving : adjusted < 0 ? IndicatorTrend.Worsening : IndicatorTrend.Stable,
            ordered.Length, delta, "Tendência descritiva; não demonstra causalidade nem substitui decisão humana.");
    }
}

public sealed class IndicatorService(IIndicatorRepository repository)
{
    public Task<IReadOnlyList<IndicatorDto>> List(Guid o, CancellationToken ct) => repository.List(RequireOrganization(o), ct);
    public Task<IndicatorDto?> Get(Guid o, Guid id, CancellationToken ct) => repository.Get(RequireOrganization(o), id, ct);
    public Task<Guid> Create(Guid o, CreateIndicatorRequest r, CancellationToken ct)
    {
        if (r.IsCalculated && string.IsNullOrWhiteSpace(r.Formula)) throw new ArgumentException("Indicador calculado exige fórmula ou regra clara.");
        return repository.Create(RequireOrganization(o), r, ct);
    }
    public Task Archive(Guid o, Guid id, CancellationToken ct) => repository.Archive(RequireOrganization(o), id, ct);
    internal static Guid RequireOrganization(Guid id) => id == Guid.Empty ? throw new InvalidOperationException("Selecione uma organização para acessar os indicadores.") : id;
}
public sealed class IndicatorTargetService(IIndicatorRepository repository)
{
    public Task<IReadOnlyList<IndicatorTargetDto>> List(Guid o, Guid? id, CancellationToken ct) => repository.Targets(IndicatorService.RequireOrganization(o), id, ct);
    public Task<Guid> Create(Guid o, Guid id, CreateTargetRequest r, CancellationToken ct)
    {
        if (r.PeriodStart >= r.PeriodEnd) throw new ArgumentException("O fim do período deve ser posterior ao início.");
        return repository.CreateTarget(IndicatorService.RequireOrganization(o), id, r, ct);
    }
}
public sealed class IndicatorMeasurementService(IIndicatorRepository repository, IndicatorTrendService trends)
{
    public Task<IReadOnlyList<IndicatorMeasurementDto>> List(Guid o, Guid id, CancellationToken ct) => repository.Measurements(IndicatorService.RequireOrganization(o), id, ct);
    public Task<Guid> Create(Guid o, Guid id, CreateMeasurementRequest r, CancellationToken ct) => repository.CreateMeasurement(IndicatorService.RequireOrganization(o), id, r, ct);
    public async Task<TrendResult> Trend(Guid o, Guid id, CancellationToken ct) => trends.Calculate(await List(o, id, ct));
}
public sealed class IndicatorAlertService(IIndicatorRepository repository)
{
    public Task<IReadOnlyList<IndicatorAlertDto>> List(Guid o, CancellationToken ct) => repository.Alerts(IndicatorService.RequireOrganization(o), ct);
    public Task Resolve(Guid o, Guid id, Guid user, CancellationToken ct) => repository.ResolveAlert(IndicatorService.RequireOrganization(o), id, user, ct);
}
public sealed class ExecutiveScorecardService(IIndicatorRepository repository)
{
    public Task<IReadOnlyList<ExecutiveScorecardDto>> List(Guid o, CancellationToken ct) => repository.Scorecards(IndicatorService.RequireOrganization(o), ct);
    public Task<Guid> Create(Guid o, Guid u, CreateScorecardRequest r, CancellationToken ct) => repository.CreateScorecard(IndicatorService.RequireOrganization(o), u, r, ct);
}
public sealed class AnalyticsSnapshotService(IIndicatorRepository repository)
{
    public Task<IReadOnlyList<AnalyticsSnapshotDto>> List(Guid o, CancellationToken ct) => repository.Snapshots(IndicatorService.RequireOrganization(o), ct);
    public Task<Guid> Create(Guid o, Guid u, string name, CancellationToken ct) => repository.CreateSnapshot(IndicatorService.RequireOrganization(o), u, name, ct);
}
public sealed class IndicatorCategoryService(IIndicatorRepository repository) { public Task<IReadOnlyList<IndicatorDto>> List(Guid o,CancellationToken ct)=>repository.List(IndicatorService.RequireOrganization(o),ct); }
public sealed class IndicatorSourceService(IIndicatorRepository repository) { public Task<IReadOnlyList<IndicatorDto>> List(Guid o,CancellationToken ct)=>repository.List(IndicatorService.RequireOrganization(o),ct); }
public sealed class IndicatorFormulaService { }
public sealed class IndicatorAlertRuleService { }
