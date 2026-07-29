namespace Valora.Application.DTOs;

public sealed record CutoverReadinessDto(Guid BatchId, string Status, IReadOnlyList<string> Checklist, IReadOnlyList<string> Blockers, IReadOnlyList<string> Warnings, string ManualCutoverPlan, string RollbackPlan);
