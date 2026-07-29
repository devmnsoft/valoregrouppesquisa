namespace Valora.Application.Migration;

public interface ILegacyImportService { Task<LegacyImportResult> ImportAsync(LegacyImportRequest request, CancellationToken cancellationToken = default); }
