using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class ExportService(IExportRepository repo, IEntitlementService ent, IAuditRepository audit) : IExportService {
    private static readonly HashSet<string> AllowedEntities = new(["responses", "results", "audit", "surveys", "forms"], StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> AllowedFormats = new(["csv", "json", "xlsx", "pdf"], StringComparer.OrdinalIgnoreCase);

    public async Task<ExportJobDto> RequestAsync(Guid organizationId, Guid? userId, ExportRequest request,
        string correlationId, CancellationToken cancellationToken) {
        if (organizationId == Guid.Empty) throw new InvalidOperationException("ORGANIZATION_SCOPE_REQUIRED");
        if (!await ent.CanUseAsync(organizationId, "exportacoes")) throw new InvalidOperationException("MODULE_NOT_ENABLED");
        var entity = request.Entity?.Trim().ToLowerInvariant() ?? string.Empty;
        var format = request.Format?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!AllowedEntities.Contains(entity)) throw new InvalidOperationException("INVALID_EXPORT_ENTITY");
        if (!AllowedFormats.Contains(format)) throw new InvalidOperationException("INVALID_EXPORT_FORMAT");

        var job = await repo.CreateAsync(organizationId, userId, entity, format, request.FilterJson, correlationId, cancellationToken);
        await audit.AddAsync(new AuditEntry(organizationId, userId, "export.requested", "export", job.Id.ToString(), "Exportação solicitada", "{}"));
        return job;
    }

    public Task<IReadOnlyList<ExportJobDto>> ListAsync(Guid organizationId, CancellationToken cancellationToken) => repo.ListAsync(organizationId, cancellationToken);
    public Task<ExportJobDto?> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken) => repo.GetAsync(organizationId, id, cancellationToken);
}
