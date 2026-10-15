using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Valora.Application.Evolution;

public sealed record EvolutionSelectionPage(IReadOnlyList<EvolutionResultOptionDto> Items, int Page, bool HasMore);

public sealed record EvolutionReevaluationPreview(long Version, PreservedEvolutionMeasurement Baseline,
    PreservedEvolutionMeasurement Current, EvolutionComparisonResult Comparison, string Fingerprint);

public static class EvolutionReevaluation {
    public static EvolutionReevaluationPreview Preview(long version, PreservedEvolutionMeasurement baseline, PreservedEvolutionMeasurement current) {
        if (current.ReferenceAt <= baseline.ReferenceAt) throw new ArgumentException("A nova medição deve ter data de referência posterior à inicial.");
        if (current.SurveyId == baseline.SurveyId && current.SourceJson == baseline.SourceJson)
            throw new ArgumentException("Selecione uma medição diferente da inicial.");
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { version, baseline.Id, current.SourceJson }))));
        return new(version, baseline, current, EvolutionComparisonService.Compare(baseline.Score, baseline.Basis, current.Score, current.Basis), fingerprint);
    }
}
