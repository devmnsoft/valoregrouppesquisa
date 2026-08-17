namespace Valora.Application.Methodology;

public sealed record MethodologyConceptDto(Guid Id, string Code, string Name, string Pillar, string Definition,
    string StrategicPurpose, string EvolutionGuidance, string[] DiagnosticQuestions, string[] RelatedIndicators,
    string[] RelatedIndices, string[] OrganizationalImpacts, string[] DeprecatedTerms, string MaturityLevel,
    string MethodologyVersion, string Status, int DisplayOrder, int Version);
public sealed record MethodologyRelationDto(Guid Id, string SourceCode, string SourceName, string TargetCode,
    string TargetName, string RelationType, decimal InfluenceWeight, string Rationale);
public sealed record MethodologyEvidenceDto(Guid Id, string ConceptCode, string PatternType, string Description,
    int MinimumOccurrences, decimal Weight);
public sealed record EvidenceItem(string Code, string Description, string Source, decimal Strength, DateTimeOffset ObservedAt);
public sealed record InferenceRequest(string Symptom, string ProbableCause, string[] Systems, IReadOnlyList<EvidenceItem> Evidence,
    string Impact, string Priority, string NextStep);
public sealed record InferenceResultDto(bool IsConclusive, string Confidence, string Symptom, string? ProbableCause,
    string[] Systems, IReadOnlyList<EvidenceItem> Evidence, string Impact, string Priority, string NextStep, string MethodologyVersion, string DataHash);

public interface IMethodologyRepository
{
    Task<IReadOnlyList<MethodologyConceptDto>> ListConceptsAsync(string? search, string? pillar, CancellationToken ct);
    Task<MethodologyConceptDto?> GetConceptAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<MethodologyRelationDto>> ListRelationsAsync(string? conceptCode, CancellationToken ct);
    Task<IReadOnlyList<MethodologyEvidenceDto>> ListEvidenceAsync(string conceptCode, CancellationToken ct);
}

public interface IMethodologyService
{
    Task<IReadOnlyList<MethodologyConceptDto>> ListConceptsAsync(string? search, string? pillar, CancellationToken ct);
    Task<MethodologyConceptDto?> GetConceptAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<MethodologyRelationDto>> ListRelationsAsync(string? conceptCode, CancellationToken ct);
    Task<IReadOnlyList<MethodologyEvidenceDto>> ListEvidenceAsync(string conceptCode, CancellationToken ct);
}
