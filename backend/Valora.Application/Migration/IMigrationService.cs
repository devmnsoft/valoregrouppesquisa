namespace Valora.Application.Migration;

public interface IMigrationService { Task<LegacyImportResult> RunAsync(LegacyImportRequest request, CancellationToken cancellationToken = default); }
