using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Valora.Application.Results;

public enum ResultLifecycleStatus { Insufficient, Calculated, Published }

public sealed record ResultEvidence(string QuestionCode, string DimensionCode, decimal NormalizedScore, int AnswerCount);
public sealed record AggregatedEvidence(IReadOnlyList<ResultEvidence> Items, int ValidAnswerCount, int ParticipantCount, bool IsAnonymous);
public sealed record ResultSnapshot(string RuleVersion, DateTimeOffset CalculatedAt, string Payload, string Sha256);
public sealed record ResultRecommendation(string DimensionCode, string Title, string Description, string Priority, IReadOnlyList<string> EvidenceCodes);
public sealed record CalculatedResult(
    ResultLifecycleStatus Status, decimal? OverallScore, IReadOnlyList<MethodologicalScoreGroup> Dimensions,
    AggregatedEvidence Evidence, IReadOnlyList<ResultRecommendation> Recommendations, ResultSnapshot Snapshot,
    string Limitation);

/// <summary>Agrega somente evidência válida e nunca devolve respostas individuais.</summary>
public sealed class EvidenceAggregationService
{
    public AggregatedEvidence Aggregate(IEnumerable<MethodologicalAnswer> answers, int participantCount, bool requiresAnonymity)
    {
        ArgumentNullException.ThrowIfNull(answers);
        if (participantCount < 0) throw new ArgumentOutOfRangeException(nameof(participantCount));
        var valid = answers.Where(IsValid).ToArray();
        var items = valid.GroupBy(x => new { x.QuestionCode, x.DimensionCode })
            .Select(group => new ResultEvidence(group.Key.QuestionCode, group.Key.DimensionCode,
                decimal.Round(group.Average(Normalize), 2), group.Count()))
            .OrderBy(x => x.DimensionCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.QuestionCode, StringComparer.OrdinalIgnoreCase).ToArray();
        return new(items, valid.Length, participantCount, requiresAnonymity);
    }

    private static bool IsValid(MethodologicalAnswer x) => x.Weight > 0m &&
        !string.IsNullOrWhiteSpace(x.QuestionCode) && !string.IsNullOrWhiteSpace(x.DimensionCode) &&
        (x.IsQualitative ? x.QualitativeNormalizedValue is >= 0m and <= 100m
            : x.RawValue is not null && x.Maximum > x.Minimum && x.RawValue >= x.Minimum && x.RawValue <= x.Maximum);
    private static decimal Normalize(MethodologicalAnswer x)
    {
        var score = x.IsQualitative ? x.QualitativeNormalizedValue!.Value : (x.RawValue!.Value - x.Minimum) / (x.Maximum - x.Minimum) * 100m;
        return x.Reverse ? 100m - score : score;
    }
}

public sealed class ValoraIndexScoreService
{
    public MethodologicalScoreResult Calculate(IEnumerable<MethodologicalAnswer> answers) => new MethodologicalScoringService().Calculate(answers);
}

public sealed class ResultSnapshotService
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ResultSnapshot Create(string ruleVersion, DateTimeOffset calculatedAt, IEnumerable<MethodologicalAnswer> answers)
    {
        if (string.IsNullOrWhiteSpace(ruleVersion)) throw new ArgumentException("A versão da regra é obrigatória.", nameof(ruleVersion));
        var ordered = answers.OrderBy(x => x.QuestionCode, StringComparer.Ordinal).ThenBy(x => x.AnswerId).ToArray();
        var payload = JsonSerializer.Serialize(ordered, Options);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return new(ruleVersion.Trim(), calculatedAt, payload, hash);
    }
}

public sealed class ResultRecommendationService
{
    public IReadOnlyList<ResultRecommendation> Build(IReadOnlyList<MethodologicalScoreGroup> dimensions, AggregatedEvidence evidence) =>
        dimensions.Where(x => x.Score < 70m).OrderBy(x => x.Score).Select(d => new ResultRecommendation(
            d.Code, $"Evoluir {d.Code}", $"Priorize a dimensão com índice {d.Score:0.##}/100 e valide o avanço no próximo ciclo.",
            d.Score < 40m ? "critical" : "high",
            evidence.Items.Where(x => string.Equals(x.DimensionCode, d.Code, StringComparison.OrdinalIgnoreCase)).Select(x => x.QuestionCode).Distinct().ToArray()))
        .Where(x => x.EvidenceCodes.Count > 0).ToArray();
}

public sealed class ResultCalculationService(
    EvidenceAggregationService evidenceService, ValoraIndexScoreService scoreService,
    ResultSnapshotService snapshotService, ResultRecommendationService recommendationService,
    ILogger<ResultCalculationService> logger)
{
    public CalculatedResult Calculate(IEnumerable<MethodologicalAnswer> input, int participantCount, int minimumParticipants,
        bool requiresAnonymity, string ruleVersion, string correlationId, DateTimeOffset? calculatedAt = null)
    {
        if (minimumParticipants <= 0) throw new ArgumentOutOfRangeException(nameof(minimumParticipants));
        var answers = input?.ToArray() ?? throw new ArgumentNullException(nameof(input));
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId, ["RuleVersion"] = ruleVersion });
        var evidence = evidenceService.Aggregate(answers, participantCount, requiresAnonymity);
        var snapshot = snapshotService.Create(ruleVersion, calculatedAt ?? DateTimeOffset.UtcNow, answers);
        if (participantCount < minimumParticipants || evidence.ValidAnswerCount == 0)
        {
            logger.LogWarning("Resultado insuficiente: {Participants}/{MinimumParticipants} participantes e {EvidenceCount} evidências válidas.", participantCount, minimumParticipants, evidence.ValidAnswerCount);
            return new(ResultLifecycleStatus.Insufficient, null, Array.Empty<MethodologicalScoreGroup>(), evidence,
                Array.Empty<ResultRecommendation>(), snapshot, "Ainda não há respostas suficientes para calcular este resultado.");
        }

        var score = scoreService.Calculate(answers);
        if (score.OverallScore is null)
            return new(ResultLifecycleStatus.Insufficient, null, score.Dimensions, evidence, [], snapshot,
                "Ainda não há respostas suficientes para calcular este resultado.");
        var recommendations = recommendationService.Build(score.Dimensions, evidence);
        logger.LogInformation("Resultado calculado com {EvidenceCount} evidências; índice {OverallScore}.", evidence.ValidAnswerCount, score.OverallScore);
        return new(ResultLifecycleStatus.Calculated, score.OverallScore, score.Dimensions, evidence, recommendations, snapshot,
            requiresAnonymity ? "As respostas são apresentadas somente de forma agregada para preservar o anonimato." : "O índice representa as evidências disponíveis no snapshot.");
    }
}

public sealed class ResultPublicationService
{
    public CalculatedResult Publish(CalculatedResult result) => result.Status switch
    {
        ResultLifecycleStatus.Insufficient => throw new InvalidOperationException("Um resultado insuficiente não pode ser publicado."),
        ResultLifecycleStatus.Published => result,
        _ => result with { Status = ResultLifecycleStatus.Published }
    };

    public void EnsureCanRecalculate(CalculatedResult result, bool createNewVersion)
    {
        if (result.Status == ResultLifecycleStatus.Published && !createNewVersion)
            throw new InvalidOperationException("Este resultado publicado preserva rastreabilidade e não pode ser alterado diretamente.");
    }
}
