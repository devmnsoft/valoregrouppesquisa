namespace Valora.Application.Migration;

public sealed record LegacyImportRequest(string Source, string PayloadJson, bool DryRun = true);
