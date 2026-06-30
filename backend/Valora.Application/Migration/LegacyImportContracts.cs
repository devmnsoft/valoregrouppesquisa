namespace Valora.Application.Migration;

public sealed record LegacyImportRequest(string Source, string PayloadJson, bool DryRun = true);
public sealed record LegacyImportResult(Guid BatchId, int Organizations, int Users, int Forms, int Surveys, int Responses, int Certificates, int Errors);

public interface ILegacyMappingService { LegacyImportResult Preview(LegacyImportRequest request); }
public interface IMigrationRepository { Task<Guid> CreateBatchAsync(string source, CancellationToken cancellationToken = default); Task AppendLogAsync(Guid batchId, string entityType, string legacyId, string status, string? error, CancellationToken cancellationToken = default); }
public interface ILegacyImportService { Task<LegacyImportResult> ImportAsync(LegacyImportRequest request, CancellationToken cancellationToken = default); }
public interface IMigrationService { Task<LegacyImportResult> RunAsync(LegacyImportRequest request, CancellationToken cancellationToken = default); }

public sealed class LegacyMappingService : ILegacyMappingService
{
    public LegacyImportResult Preview(LegacyImportRequest request) => new(Guid.Empty, Count(request.PayloadJson, "companies"), Count(request.PayloadJson, "users"), Count(request.PayloadJson, "forms"), Count(request.PayloadJson, "surveys"), Count(request.PayloadJson, "responses"), Count(request.PayloadJson, "certificates"), 0);
    private static int Count(string payload, string marker) => string.IsNullOrWhiteSpace(payload) || !payload.Contains(marker, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
}
