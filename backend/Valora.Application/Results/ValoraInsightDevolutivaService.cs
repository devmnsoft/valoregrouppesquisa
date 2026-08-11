namespace Valora.Application.Results;

public sealed class ValoraInsightDevolutivaService
{
    public const string InsufficientEvidenceMessage =
        "As informações disponíveis ainda não permitem concluir essa análise com segurança. Recomenda-se ampliar a coleta de evidências antes de definir uma causa ou prioridade.";

    public ValoraAnalysis Analyze(
        string observacao,
        IEnumerable<string>? evidencias,
        string? correlacao = null,
        string? causaProvavel = null,
        string? impacto = null,
        string? prioridade = null,
        IEnumerable<string>? plano = null,
        string? limites = null)
    {
        var evidence = (evidencias ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct().ToArray();
        // A metodologia exige três sinais convergentes antes de permitir qualquer
        // conclusão. Correlação continua sendo hipótese e nunca prova causalidade.
        var confidence = EvidenceConfidence.Classify(evidence.Length);
        var sufficient = EvidenceConfidence.AllowsConclusion(evidence.Length) && !string.IsNullOrWhiteSpace(correlacao);
        if (!sufficient)
            return new(observacao, evidence, null, null, null, null, Array.Empty<string>(), InsufficientEvidenceMessage, false, confidence);

        return new(observacao, evidence, correlacao, causaProvavel, impacto, prioridade,
            (plano ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray(),
            string.IsNullOrWhiteSpace(limites) ? "A correlação observada não comprova causalidade; valide a hipótese no próximo ciclo." : limites,
            true, confidence);
    }

    public string Build(ValoraInsightResult result)
    {
        var analysis = Analyze(
            $"Pontuação consolidada de {result.TotalScore}/{result.MaxScore} no nível {result.Level}.",
            Array.Empty<string>());
        return $"Observacao: {analysis.Observacao}\nEvidencias: nenhuma evidência detalhada foi fornecida.\nCorrelação: não calculada.\nCausa provável: não definida.\nImpacto organizacional: não definido.\nPrioridade: não definida.\nPlano de evolução: ampliar e validar a coleta.\nLimites da análise: {analysis.LimitesDaAnalise}";
    }
}
