namespace Valora.Application.DTOs;

public sealed record MigrationSummaryDto(int TotalRead, int Valid, int Invalid, int WouldInsert, int WouldUpdate, int WouldSkip, int Conflicts, IReadOnlyList<string> UnmappedFields, IReadOnlyList<string> SensitiveDataDetected, IReadOnlyList<string> Risks);
