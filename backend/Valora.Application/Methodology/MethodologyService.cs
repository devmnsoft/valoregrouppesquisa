namespace Valora.Application.Methodology;

public sealed class MethodologyService(IMethodologyRepository repository) : IMethodologyService {
    public Task<IReadOnlyList<MethodologyConceptDto>> ListConceptsAsync(string? search, string? pillar, CancellationToken ct) => repository.ListConceptsAsync(search, pillar, ct);
    public Task<MethodologyConceptDto?> GetConceptAsync(string code, CancellationToken ct) => repository.GetConceptAsync(code, ct);
    public Task<IReadOnlyList<MethodologyRelationDto>> ListRelationsAsync(string? conceptCode, CancellationToken ct) => repository.ListRelationsAsync(conceptCode, ct);
    public Task<IReadOnlyList<MethodologyEvidenceDto>> ListEvidenceAsync(string conceptCode, CancellationToken ct) => repository.ListEvidenceAsync(conceptCode, ct);
}

public sealed class ValoraInferenceEngine {
    public InferenceResultDto Infer(InferenceRequest request) {
        var evidence = request.Evidence.Where(x => x.Strength > 0).GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase).Select(x => x.OrderByDescending(e => e.Strength).First()).ToList();
        var distinctSources = evidence.Select(x => x.Source).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var conclusive = evidence.Count >= 3 && distinctSources >= 2;
        var confidence = (evidence.Count, distinctSources) switch {
            (< 3, _) => "Baixa",
            ( >= 3, < 2) => "Baixa", // Sem convergência entre fontes distintas
            ( >= 6, >= 3) => "Muito Alta",
            ( >= 4, >= 2) => "Alta",
            _ => "Moderada" // count >= 3 && distinctSources >= 2
        };
        var canonical = string.Join('|', evidence.OrderBy(x => x.Code).Select(x => $"{x.Code}:{x.Strength:0.####}:{x.ObservedAt:O}"));
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        return new(conclusive, confidence, request.Symptom, conclusive ? request.ProbableCause : null, request.Systems,
            evidence, conclusive ? request.Impact : "Dados insuficientes para estimar impacto.", conclusive ? request.Priority : "Aguardar evidências convergentes e independentes",
            conclusive ? request.NextStep : "Ampliar a coleta até obter ao menos três evidências convergentes provenientes de ao menos duas fontes independentes.", "1.0", hash);
    }
}
