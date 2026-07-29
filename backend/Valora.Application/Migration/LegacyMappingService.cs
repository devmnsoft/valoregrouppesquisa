namespace Valora.Application.Migration;

public sealed class LegacyMappingService : ILegacyMappingService
{
    public LegacyImportResult Preview(LegacyImportRequest request) => new(Guid.Empty, Count(request.PayloadJson, "companies"), Count(request.PayloadJson, "users"), Count(request.PayloadJson, "forms"), Count(request.PayloadJson, "surveys"), Count(request.PayloadJson, "responses"), Count(request.PayloadJson, "certificates"), 0);
    private static int Count(string payload, string marker) => string.IsNullOrWhiteSpace(payload) || !payload.Contains(marker, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
}
