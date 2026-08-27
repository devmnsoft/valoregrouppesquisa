using Valora.Application.OrganizationalIntelligence;

namespace Valora.Application.Intelligence;

public sealed class EvidenceMatrixService
{
    public IReadOnlyList<IntelligenceEvidence> Build(IEnumerable<EvidenceItemDto> items) => items
        .Where(x => string.Equals(x.MappingStatus, "mapped", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(x.DimensionCode))
        .Select(x => new IntelligenceEvidence(x.Id, x.SourceType, x.DimensionCode, x.ConceptCode,
            x.NormalizedValue, Math.Clamp(x.ConfidenceWeight, 0, 1),
            x.TextExcerpt ?? x.RawValueMasked ?? "Evidência quantitativa rastreável."))
        .ToList();
}
