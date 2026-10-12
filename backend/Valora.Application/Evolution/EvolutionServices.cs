using Valora.Application.OrganizationalIntelligence;

namespace Valora.Application.Evolution;

public sealed record EvolutionCycleDto(Guid Id, Guid? DiagnosticId, Guid? ResultId, string Title, string Summary, string Status, decimal? BaselineScore, decimal? CurrentScore, decimal? TargetScore, string? BaselineLevel, string? CurrentLevel, string Trend, DateTime PeriodStart, DateTime? PeriodEnd, string EvidenceSummary, string? LearningSummary, string BaselineSource, string? MethodologyVersion, DateTime CreatedAt);
public sealed record EvolutionSnapshotDto(Guid Id, Guid EvolutionCycleId, string SnapshotType, decimal? Score, decimal? PreviousScore, decimal? Delta, string? Level, string Trend, int CompletedActions, int DelayedActions, int OpenAlerts, string EvidenceSummary, string Interpretation, string Recommendation, DateTime CalculatedAt);
public sealed record EvolutionResultOptionDto(Guid ResultId, Guid DiagnosticId, string DiagnosticTitle, decimal Score, decimal MaxScore, decimal Percentage, string MethodologyVersion, DateTime CalculatedAt, int ScoredResponseCount, string ResultType = "organizational_measurement");
public sealed record OpenEvolutionCycleRequest(Guid? DiagnosticId, Guid? ResultId, Guid? GovernanceCycleId, string Title, string Summary, decimal? BaselineScore, decimal? TargetScore, string? BaselineLevel, DateTime PeriodStart, DateTime? PeriodEnd, string EvidenceSummary, string? MethodologyVersion = null, string? PopulationSignature = null, string? DimensionSignature = null, string? ScaleSignature = null, string? CalculationCriteria = null, string BaselineSource = "manual_informed");
public interface IEvolutionRepository { Task<IReadOnlyList<EvolutionCycleDto>> List(Guid o, CancellationToken c); Task<EvolutionCycleDto?> Get(Guid o, Guid id, CancellationToken c); Task<IReadOnlyList<EvolutionSnapshotDto>> Snapshots(Guid o, Guid cycle, CancellationToken c); Task<Guid> Open(Guid o, Guid u, OpenEvolutionCycleRequest r, CancellationToken c); Task<Guid> Snapshot(Guid o, Guid u, Guid cycle, string evidence, string interpretation, string recommendation, CancellationToken c); Task Close(Guid o, Guid u, Guid cycle, string learning, CancellationToken c); }
public interface IEvolutionCycleService { Task<IReadOnlyList<EvolutionCycleDto>> List(Guid o, CancellationToken c); Task<EvolutionCycleDto?> Get(Guid o, Guid id, CancellationToken c); Task<IReadOnlyList<EvolutionResultOptionDto>> ResultOptions(Guid o, Guid? diagnosticId, CancellationToken c); Task<Guid> Open(Guid o, Guid u, OpenEvolutionCycleRequest x, CancellationToken c); Task Close(Guid o, Guid u, Guid id, string learning, CancellationToken c); }
public sealed class EvolutionCycleService(IEvolutionRepository r, IOrganizationalIntelligenceRepository intelligence) : IEvolutionCycleService {
    public Task<IReadOnlyList<EvolutionCycleDto>> List(Guid o, CancellationToken c) => r.List(o, c);
    public Task<EvolutionCycleDto?> Get(Guid o, Guid id, CancellationToken c) => r.Get(o, id, c);
    public async Task<IReadOnlyList<EvolutionResultOptionDto>> ResultOptions(Guid o, Guid? diagnosticId, CancellationToken c) {
        var readings = await intelligence.ListEvolutionComparisonSurveysAsync(o, c);
        return readings
            .Where(x => diagnosticId is null || x.SurveyId == diagnosticId)
            .Select(x => new EvolutionResultOptionDto(x.SurveyId, x.SurveyId, x.Title, x.MaturityIndex ?? 0, 100, x.MaturityIndex ?? 0, MethodologyVersion(x.SnapshotJson), x.CapturedAt, x.ScoredResponseCount))
            .ToList();
    }
    public async Task<Guid> Open(Guid o, Guid u, OpenEvolutionCycleRequest x, CancellationToken c) {
        if (o == Guid.Empty || u == Guid.Empty) throw new UnauthorizedAccessException("Organização e autor são obrigatórios.");
        if (string.IsNullOrWhiteSpace(x.Title) || x.Title.Trim().Length is < 3 or > 160) throw new ArgumentException("O nome do ciclo deve ter entre 3 e 160 caracteres.");
        if (string.IsNullOrWhiteSpace(x.Summary) || x.Summary.Trim().Length > 1000) throw new ArgumentException("O objetivo do ciclo é obrigatório e aceita até 1.000 caracteres.");
        if (string.IsNullOrWhiteSpace(x.EvidenceSummary) || x.EvidenceSummary.Trim().Length > 2000) throw new ArgumentException("Baseline exige evidência com até 2.000 caracteres.");
        if (x.PeriodEnd < x.PeriodStart) throw new ArgumentException("A data final deve ser posterior à data inicial.");
        if (x.TargetScore is < 0 or > 100) throw new ArgumentException("A meta deve estar entre 0 e 100.");
        var normalized = x with { Title = x.Title.Trim(), Summary = x.Summary.Trim(), EvidenceSummary = x.EvidenceSummary.Trim() };
        if (x.ResultId is { } selected) {
            var reading = (await intelligence.ListEvolutionComparisonSurveysAsync(o, c)).FirstOrDefault(item => item.SurveyId == selected);
            if (reading is null) throw new ArgumentException("Selecione uma medição organizacional disponível para esta organização.");
            if (x.DiagnosticId is { } diagnosticId && diagnosticId != reading.SurveyId) throw new ArgumentException("A medição selecionada não pertence ao diagnóstico informado.");
            if (!reading.MaturityIndex.HasValue) throw new ArgumentException("A medição selecionada ainda não possui resultado organizacional calculado.");
            normalized = normalized with {
                DiagnosticId = reading.SurveyId,
                ResultId = reading.SurveyId,
                BaselineScore = reading.MaturityIndex,
                BaselineLevel = "Medição organizacional",
                MethodologyVersion = MethodologyVersion(reading.SnapshotJson),
                PopulationSignature = reading.PopulationSignature,
                DimensionSignature = string.Join("|", reading.MeasuredDimensionCodes.OrderBy(code => code, StringComparer.Ordinal)),
                ScaleSignature = ScaleSignature(reading.SnapshotJson, reading.MeasuredDimensionCodes),
                CalculationCriteria = CalculationCriteria(reading.CriteriaRows, reading.MeasuredDimensionCodes),
                BaselineSource = "organizational_measurement",
                EvidenceSummary = $"{normalized.EvidenceSummary} Medição inicial selecionada: {reading.Title}, {reading.MaturityIndex:0.0} pontos, {reading.ScoredResponseCount} resposta(s) pontuada(s), capturada em {reading.CapturedAt:dd/MM/yyyy}."
            };
        }
        else if (x.BaselineScore is < 0 or > 100) throw new ArgumentException("Baseline e meta devem estar entre 0 e 100.");
        return await r.Open(o, u, normalized, c);
    }
    public Task Close(Guid o, Guid u, Guid id, string learning, CancellationToken c) => r.Close(o, u, id, learning, c);

    private static string MethodologyVersion(string snapshotJson) {
        try {
            using var document = System.Text.Json.JsonDocument.Parse(snapshotJson);
            if (!document.RootElement.TryGetProperty("methodology", out var methodology) || methodology.ValueKind != System.Text.Json.JsonValueKind.Object) return "indefinida";
            var code = methodology.TryGetProperty("code", out var codeValue) && codeValue.ValueKind == System.Text.Json.JsonValueKind.String ? codeValue.GetString() ?? string.Empty : string.Empty;
            var version = methodology.TryGetProperty("version", out var versionValue) ? versionValue.ToString() : string.Empty;
            return code.Length > 0 && version.Length > 0 ? $"{code}@{version}" : "indefinida";
        }
        catch (System.Text.Json.JsonException) { return "indefinida"; }
    }
    private static string ScaleSignature(string snapshotJson, IReadOnlyList<string> measuredDimensions) {
        var measured = measuredDimensions.ToHashSet(StringComparer.Ordinal);
        var scales = new List<string>();
        try {
            using var document = System.Text.Json.JsonDocument.Parse(snapshotJson);
            if (document.RootElement.TryGetProperty("questions", out var questions) && questions.ValueKind == System.Text.Json.JsonValueKind.Array)
                foreach (var question in questions.EnumerateArray())
                    if (question.TryGetProperty("dimension", out var dimension) && measured.Contains(dimension.GetString() ?? string.Empty) && question.TryGetProperty("scale", out var scale))
                        scales.Add(scale.GetRawText());
        }
        catch (System.Text.Json.JsonException) { }
        return string.Join("|", scales.Distinct(StringComparer.Ordinal).OrderBy(scale => scale, StringComparer.Ordinal));
    }
    private static string CalculationCriteria(IReadOnlyList<EvolutionQuestionCriterionRow> rows, IReadOnlyList<string> measuredDimensions) {
        var measured = measuredDimensions.ToHashSet(StringComparer.Ordinal);
        return string.Join(';', rows.Where(row => !string.IsNullOrWhiteSpace(row.DimensionCode) && measured.Contains(row.DimensionCode!))
            .Select(row => $"{row.DimensionCode}|{row.MetricCode ?? "-"}|{row.IndexCode ?? "-"}|{row.WeightText}|{row.Polarity}"));
    }
}
public sealed class EvolutionSnapshotService(IEvolutionRepository r) { public Task<IReadOnlyList<EvolutionSnapshotDto>> List(Guid o, Guid id, CancellationToken c) => r.Snapshots(o, id, c); public Task<Guid> Generate(Guid o, Guid u, Guid id, string evidence, string interpretation, string recommendation, CancellationToken c) { if (o == Guid.Empty || u == Guid.Empty) throw new UnauthorizedAccessException("Organização e autor são obrigatórios."); if (string.IsNullOrWhiteSpace(evidence) || string.IsNullOrWhiteSpace(interpretation) || string.IsNullOrWhiteSpace(recommendation)) throw new ArgumentException("Evidência, interpretação e próximo passo são obrigatórios."); if (evidence.Trim().Length > 2000 || interpretation.Trim().Length > 2000 || recommendation.Trim().Length > 2000) throw new ArgumentException("Evidência, interpretação e recomendação aceitam até 2.000 caracteres."); return r.Snapshot(o, u, id, evidence.Trim(), interpretation.Trim(), recommendation.Trim(), c); } }
public sealed class EvolutionMeasurementService { public static string Trend(decimal? previous, decimal? current, int evidenceCount) { if (evidenceCount < 2 || previous is null || current is null) return "insufficient_sample"; var d = current - previous; return d >= 5 ? "consistent_evolution" : d > 0 ? "gradual_evolution" : d == 0 ? "stability" : d <= -3 ? "regression" : "oscillation"; } }

public sealed record EvolutionComparisonBasis(string MethodologyVersion, string DimensionSignature, string ScaleSignature, string CalculationCriteria, string PopulationSignature, bool DataAvailable);
public sealed record EvolutionComparisonResult(bool IsComparable, decimal? AbsoluteVariation, string Limitation);
public static class EvolutionComparisonService {
    public static EvolutionComparisonResult Compare(decimal? baseline, EvolutionComparisonBasis baselineBasis, decimal? current, EvolutionComparisonBasis currentBasis) {
        if (!baselineBasis.DataAvailable || !currentBasis.DataAvailable || baseline is null || current is null)
            return new(false, null, "Os resultados não estão disponíveis para comparação.");
        var differences = new List<string>();
        if (!Same(baselineBasis.MethodologyVersion, currentBasis.MethodologyVersion)) differences.Add("versão metodológica");
        if (!Same(baselineBasis.DimensionSignature, currentBasis.DimensionSignature)) differences.Add("dimensões");
        if (!Same(baselineBasis.ScaleSignature, currentBasis.ScaleSignature)) differences.Add("escala");
        if (!Same(baselineBasis.CalculationCriteria, currentBasis.CalculationCriteria)) differences.Add("critérios de cálculo");
        if (!Same(baselineBasis.PopulationSignature, currentBasis.PopulationSignature)) differences.Add("população ou recorte");
        return differences.Count == 0
            ? new(true, current - baseline, "Variação absoluta observada; não demonstra causalidade do plano.")
            : new(false, null, $"Comparação indisponível: diferem {string.Join(", ", differences)}. Os resultados devem ser analisados separadamente.");
    }
    private static bool Same(string left, string right) => !string.IsNullOrWhiteSpace(left) && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
public sealed class OpenEvolutionCycleUseCase(EvolutionCycleService s) { public Task<Guid> Execute(Guid o, Guid u, OpenEvolutionCycleRequest r, CancellationToken c) => s.Open(o, u, r, c); }
public sealed class CloseEvolutionCycleUseCase(EvolutionCycleService s) { public Task Execute(Guid o, Guid u, Guid id, string l, CancellationToken c) => s.Close(o, u, id, l, c); }
public sealed class GenerateEvolutionSnapshotUseCase(EvolutionSnapshotService s) { public Task<Guid> Execute(Guid o, Guid u, Guid id, string e, string i, string r, CancellationToken c) => s.Generate(o, u, id, e, i, r, c); }
