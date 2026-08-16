namespace Valora.Application.DiagnosticWorkspace;

public sealed class DiagnosticWorkspaceService(IDiagnosticWorkspaceRepository repository) : IDiagnosticWorkspaceService
{
    public async Task<DiagnosticWorkspaceOverviewDto?> GetOverviewAsync(Guid organizationId, Guid id, CancellationToken ct)
    {
        var cycle = await repository.GetCycleAsync(organizationId, id, ct);
        if (cycle is null) return null;
        var indices = await repository.ModuleAsync(organizationId, cycle.Id, cycle.SurveyId, "indices", ct);
        var insights = await repository.ModuleAsync(organizationId, cycle.Id, cycle.SurveyId, "insights", ct);
        var actions = await repository.ModuleAsync(organizationId, cycle.Id, cycle.SurveyId, "actions", ct);
        var alerts = await repository.ModuleAsync(organizationId, cycle.Id, cycle.SurveyId, "alerts", ct);
        return new(cycle, await repository.LastJobAsync(organizationId, cycle.SurveyId, ct), indices.Take(4).ToList(), insights.Take(3).ToList(), actions.Take(3).ToList(), alerts.Take(3).ToList());
    }
    public async Task<DiagnosticWorkspaceDto?> GetWorkspaceAsync(Guid organizationId, Guid id, CancellationToken ct)
    {
        var overview = await GetOverviewAsync(organizationId, id, ct); if (overview is null) return null;
        var c = overview.Cycle;
        return new(overview, await repository.EvidenceAsync(organizationId, c.SurveyId, ct),
            await repository.ModuleAsync(organizationId,c.Id,c.SurveyId,"metrics",ct), await repository.ModuleAsync(organizationId,c.Id,c.SurveyId,"indices",ct),
            await repository.ModuleAsync(organizationId,c.Id,c.SurveyId,"inferences",ct), await repository.ModuleAsync(organizationId,c.Id,c.SurveyId,"insights",ct),
            await repository.ModuleAsync(organizationId,c.Id,c.SurveyId,"actions",ct));
    }
    public async Task<IReadOnlyList<DiagnosticWorkspaceEvidenceDto>?> GetEvidenceAsync(Guid o, Guid id, CancellationToken ct) { var c=await repository.GetCycleAsync(o,id,ct); return c is null?null:await repository.EvidenceAsync(o,c.SurveyId,ct); }
    public async Task<IReadOnlyList<DiagnosticWorkspaceItemDto>?> GetModuleAsync(Guid o, Guid id, string module, CancellationToken ct) { var c=await repository.GetCycleAsync(o,id,ct); return c is null?null:await repository.ModuleAsync(o,c.Id,c.SurveyId,module,ct); }
    public async Task<DiagnosticWorkspaceCommandDto?> ProcessAsync(Guid o, Guid id, Guid user, string correlation, CancellationToken ct) { var c=await repository.GetCycleAsync(o,id,ct); if(c is null)return null; if(await repository.HasActiveJobAsync(o,c.SurveyId,ct)) return new(c.Id,"processing","Já existe um processamento em andamento para este diagnóstico."); var job=await repository.MarkProcessingAsync(o,c,user,correlation,ct); return new(c.Id,"processing","A Inteligência Organizacional iniciou o processamento deste ciclo.",job); }
    public async Task<DiagnosticWorkspaceCommandDto?> CloseCycleAsync(Guid o, Guid id, Guid user, string correlation, CancellationToken ct) { var c=await repository.GetCycleAsync(o,id,ct); return c is null?null:await repository.CloseAsync(o,c,user,correlation,ct); }
    public async Task<DiagnosticWorkspaceCommandDto?> GenerateReportAsync(Guid o, Guid id, Guid user, bool preview, CancellationToken ct) { var c=await repository.GetCycleAsync(o,id,ct); return c is null?null:await repository.GenerateReportAsync(o,c,user,preview,ct); }
}
