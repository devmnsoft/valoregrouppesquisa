namespace Valora.Application.DTOs;

public sealed record LegacyImportDiffDto(string Entity, string LegacyId, string Field, string LegacyMaskedValue, string CurrentMaskedValue, string Severity);
