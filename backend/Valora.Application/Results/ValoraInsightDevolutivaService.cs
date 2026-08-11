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
        // Uma única observação não demonstra correlação nem causalidade. Duas fontes são
        // o mínimo técnico para permitir uma hipótese, nunca uma afirmação causal.
        var sufficient = evidence.Length >= 2 && !string.IsNullOrWhiteSpace(correlacao);
        if (!sufficient)
            return new(observacao, evidence, null, null, null, null, Array.Empty<string>(), InsufficientEvidenceMessage, false);

        return new(observacao, evidence, correlacao, causaProvavel, impacto, prioridade,
            (plano ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray(),
            string.IsNullOrWhiteSpace(limites) ? "A correlação observada não comprova causalidade; valide a hipótese no próximo ciclo." : limites,
            true);
    }

    public string Build(ValoraInsightResult result)
    {
        var analysis = Analyze(
            $"Pontuação consolidada de {result.TotalScore}/{result.MaxScore} no nível {result.Level}.",
            Array.Empty<string>());
        return $"Observacao: {analysis.Observacao}\nEvidencias: nenhuma evidência detalhada foi fornecida.\nCorrelação: não calculada.\nCausa provável: não definida.\nImpacto organizacional: não definido.\nPrioridade: não definida.\nPlano de evolução: ampliar e validar a coleta.\nLimites da análise: {analysis.LimitesDaAnalise}";
    }
}
