namespace Valora.Application.Heatmap;

public sealed class HeatmapCalculationService
{
    public const int MinimumSample = 5;
    public static string Level(decimal? score, int sample) => sample < MinimumSample ? "amostra insuficiente" : score switch
    { >= 80 => "excelente", >= 65 => "saudável", >= 50 => "em atenção", >= 35 => "crítico", _ => "muito crítico" };
    public static string Risk(decimal? score, int sample) => sample < MinimumSample ? "indeterminado" : score switch
    { >= 80 => "baixo", >= 65 => "moderado", >= 50 => "atenção", >= 35 => "alto", _ => "muito alto" };
}

public sealed class HeatmapAiInterpretationService
{
    public string Interpret(IReadOnlyList<HeatmapCellDto> cells) => cells.Count == 0
        ? "Não há evidências agregadas suficientes para interpretação."
        : "Leitura descritiva baseada exclusivamente nos scores e amostras agregadas. Os sinais indicam onde investigar e não atribuem culpa nem causa.";
}

public sealed class HeatmapQueryService(IHeatmapRepository repository)
{
    public Task<IReadOnlyList<HeatmapSnapshotDto>> OverviewAsync(Guid organizationId, CancellationToken ct) => repository.ListAsync(organizationId, ct);
    public Task<HeatmapSnapshotDto?> GetAsync(Guid organizationId, Guid id, CancellationToken ct) => repository.GetAsync(organizationId, id, ct);
}

public sealed class HeatmapService(IHeatmapRepository repository) : IHeatmapService
{
    public Task<IReadOnlyList<HeatmapSnapshotDto>> OverviewAsync(Guid organizationId, CancellationToken ct) => repository.ListAsync(organizationId, ct);
    public Task<HeatmapSnapshotDto?> GetAsync(Guid organizationId, Guid id, CancellationToken ct) => repository.GetAsync(organizationId, id, ct);
    public Task<HeatmapSnapshotDto> GenerateAsync(Guid organizationId, Guid? userId, GenerateHeatmapRequest r, CancellationToken ct)
    {
        if (r.DiagnosticId == Guid.Empty) throw new ArgumentException("Selecione um diagnóstico real.");
        if (r.ViewBy is not ("dimension" or "index" or "area" or "unit" or "leadership" or "period"))
            throw new ArgumentException("Recorte de heatmap inválido.");
        return repository.GenerateAsync(organizationId, userId, new(r.DiagnosticId,r.ResultId,r.ViewBy,r.Area,r.Unit,r.Leadership,r.IndexCode,r.PeriodStart,r.PeriodEnd), ct);
    }
}

public sealed class GenerateHeatmapSnapshotUseCase(IHeatmapService service)
{ public Task<HeatmapSnapshotDto> ExecuteAsync(Guid org, Guid? user, GenerateHeatmapRequest request, CancellationToken ct) => service.GenerateAsync(org,user,request,ct); }
public sealed class GetHeatmapOverviewUseCase(IHeatmapService service)
{ public Task<IReadOnlyList<HeatmapSnapshotDto>> ExecuteAsync(Guid org, CancellationToken ct) => service.OverviewAsync(org,ct); }
