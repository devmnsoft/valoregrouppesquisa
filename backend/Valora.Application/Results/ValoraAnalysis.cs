namespace Valora.Application.Results;

/// <summary>Uma leitura explicável: cada inferência preserva evidências, limites e próximo passo.</summary>
public sealed record ValoraAnalysis(
    string Observacao,
    IReadOnlyList<string> Evidencias,
    string? Correlacao,
    string? CausaProvavel,
    string? ImpactoOrganizacional,
    string? Prioridade,
    IReadOnlyList<string> PlanoDeEvolucao,
    string LimitesDaAnalise,
    bool EvidenciasSuficientes);
