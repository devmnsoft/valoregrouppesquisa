using Valora.Application.OrganizationalIntelligence;
using Valora.Application.Common;
using Valora.Application.Access;
using Valora.Application.ModularSaas;

namespace Valora.Application.Evolution;

public sealed record EvolutionCycleDto(Guid Id, Guid? DiagnosticId, Guid? ResultId, string Title, string Summary, string Status, decimal? BaselineScore, decimal? CurrentScore, decimal? TargetScore, string? BaselineLevel, string? CurrentLevel, string Trend, DateTime PeriodStart, DateTime? PeriodEnd, string EvidenceSummary, string? LearningSummary, string BaselineSource, string? MethodologyVersion, DateTime CreatedAt, string? PendingTreatment = null);
public sealed record EvolutionSnapshotDto(Guid Id, Guid EvolutionCycleId, string SnapshotType, decimal? Score, decimal? PreviousScore, decimal? Delta, string? Level, string Trend, int CompletedActions, int DelayedActions, int OpenAlerts, string EvidenceSummary, string Interpretation, string Recommendation, DateTime CalculatedAt, string MetadataJson = "{}");
public sealed record EvolutionResultOptionDto(Guid SourceSurveyId, string DiagnosticTitle, decimal Score, decimal MaxScore, decimal Percentage, string MethodologyVersion, DateTime CalculatedAt, int ScoredResponseCount, string ResultType = "organizational_aggregate_candidate");
public sealed record OpenEvolutionCycleRequest(Guid? DiagnosticId, Guid? ResultId, Guid? GovernanceCycleId, string Title, string Summary, decimal? BaselineScore, decimal? TargetScore, string? BaselineLevel, DateTime PeriodStart, DateTime? PeriodEnd, string EvidenceSummary, string? MethodologyVersion = null, string? PopulationSignature = null, string? DimensionSignature = null, string? ScaleSignature = null, string? CalculationCriteria = null, string BaselineSource = "manual_informed", Guid? SourceSurveyId = null, PreservedEvolutionMeasurement? Measurement = null, string? ReferenceMode = null);
public interface IEvolutionRepository {
    Task<Guid> ReviewReference(Guid o, Guid u, Guid cycle, long version, string evidence, string nextStep, string commandId, CancellationToken c) => throw new NotSupportedException();
    Task<bool> LinksAvailable(Guid o, Guid? diagnosticId, Guid? governanceCycleId, CancellationToken c) => throw new NotSupportedException();
    Task<Guid?> ResultSurvey(Guid o, Guid resultId, CancellationToken c) => throw new NotSupportedException();
    Task<Guid?> DiagnosticSurvey(Guid o, Guid diagnosticId, CancellationToken c) => throw new NotSupportedException();
    Task<IReadOnlyList<EvolutionCycleDto>> List(Guid o, CancellationToken c); Task<EvolutionCycleDto?> Get(Guid o, Guid id, CancellationToken c); Task<IReadOnlyList<EvolutionSnapshotDto>> Snapshots(Guid o, Guid cycle, CancellationToken c); Task<Guid> Open(Guid o, Guid u, OpenEvolutionCycleRequest r, CancellationToken c); Task<Guid> Snapshot(Guid o, Guid u, Guid cycle, string evidence, string interpretation, string recommendation, CancellationToken c); Task Close(Guid o, Guid u, Guid cycle, string learning, CancellationToken c);
    Task<PreservedEvolutionMeasurement?> Baseline(Guid o, Guid cycle, CancellationToken c) => throw new NotSupportedException();
    Task<long> Version(Guid o, Guid cycle, CancellationToken c) => throw new NotSupportedException();
    Task<Guid?> ReevaluationReplay(Guid o, Guid cycle, string commandId, string fingerprint, Guid surveyId, long version, CancellationToken c) => throw new NotSupportedException();
    Task<Guid> Reevaluate(Guid o, Guid u, Guid cycle, EvolutionReevaluationPreview preview, string commandId, CancellationToken c) => throw new NotSupportedException();
    Task CloseReviewed(Guid o, Guid u, Guid cycle, long version, string learning, string pendingTreatment, CancellationToken c) => throw new NotSupportedException();
}
public interface IEvolutionCycleService {
    Task<Guid> ReviewReference(Guid o, Guid u, Guid cycle, long version, string evidence, string nextStep, string commandId, CancellationToken c) => throw new NotSupportedException();
    Task<IReadOnlyList<EvolutionCycleDto>> List(Guid o, CancellationToken c); Task<EvolutionCycleDto?> Get(Guid o, Guid id, CancellationToken c); Task<IReadOnlyList<EvolutionResultOptionDto>> ResultOptions(Guid o, Guid? diagnosticId, CancellationToken c); Task<Guid> Open(Guid o, Guid u, OpenEvolutionCycleRequest x, CancellationToken c); Task Close(Guid o, Guid u, Guid id, string learning, CancellationToken c);
    Task<EvolutionSelectionPage> SelectionPage(Guid o, Guid? diagnosticId, int page, CancellationToken c);
    Task<EvolutionResultOptionDto?> SelectedOption(Guid o, Guid surveyId, CancellationToken c);
    Task<PreservedEvolutionMeasurement?> Baseline(Guid o, Guid cycle, CancellationToken c);
    Task<long> Version(Guid o, Guid cycle, CancellationToken c);
    Task<EvolutionReevaluationPreview> Preview(Guid o, Guid u, Guid cycle, Guid surveyId, CancellationToken c);
    Task<Guid> ConfirmReevaluation(Guid o, Guid u, Guid cycle, Guid surveyId, long version, string fingerprint, string commandId, CancellationToken c);
    Task CloseReviewed(Guid o, Guid u, Guid cycle, long version, string learning, string pendingTreatment, CancellationToken c);
}
public sealed class EvolutionCycleService(IEvolutionRepository r, IOrganizationalIntelligenceRepository intelligence, ICurrentRequestContext context, ICommercialSaasRepository commercial) : IEvolutionCycleService {
    public async Task<Guid> ReviewReference(Guid o, Guid u, Guid cycle, long version, string evidence, string nextStep, string commandId, CancellationToken c) {
        await Authorize(o, u, c);
        if (version < 1 || !Guid.TryParseExact(commandId, "N", out _)) throw new ArgumentException("A revisão exige versão e identificador de comando válidos.");
        if (string.IsNullOrWhiteSpace(evidence) || evidence.Trim().Length is < 3 or > 2000 || string.IsNullOrWhiteSpace(nextStep) || nextStep.Trim().Length is < 3 or > 2000)
            throw new ArgumentException("Registre as evidências verificadas e o próximo passo com 3 a 2.000 caracteres.");
        return await r.ReviewReference(o, u, cycle, version, evidence.Trim(), nextStep.Trim(), commandId, c);
    }
    public Task<IReadOnlyList<EvolutionCycleDto>> List(Guid o, CancellationToken c) { AuthorizeRead(o); return r.List(o, c); }
    public Task<EvolutionCycleDto?> Get(Guid o, Guid id, CancellationToken c) { AuthorizeRead(o); return r.Get(o, id, c); }
    public async Task<IReadOnlyList<EvolutionResultOptionDto>> ResultOptions(Guid o, Guid? diagnosticId, CancellationToken c) {
        return (await SelectionPage(o, diagnosticId, 1, c)).Items;
    }
    public async Task<EvolutionResultOptionDto?> SelectedOption(Guid o, Guid surveyId, CancellationToken c) {
        AuthorizeRead(o);
        var reading = await intelligence.GetEvolutionSurveyAsync(o, surveyId, c);
        return reading?.MaturityIndex is { } score ? new(reading.SurveyId, reading.Title, score, 100, score, EvolutionMeasurementBasis.Capture(reading).Basis.MethodologyVersion, reading.CapturedAt, reading.ScoredResponseCount) : null;
    }
    public async Task<EvolutionSelectionPage> SelectionPage(Guid o, Guid? diagnosticId, int page, CancellationToken c) {
        AuthorizeRead(o);
        var surveyId = diagnosticId is { } reference ? await r.DiagnosticSurvey(o, reference, c) : null;
        if (diagnosticId.HasValue && surveyId is null) return new([], Math.Max(1, page), false);
        var readings = await intelligence.SelectEvolutionSurveysAsync(o, surveyId, page, 20, c);
        var options = readings
            .Where(x => x.MaturityIndex is >= 0 and <= 100)
            .Select(x => new EvolutionResultOptionDto(x.SurveyId, x.Title, x.MaturityIndex!.Value, 100, x.MaturityIndex.Value, EvolutionMeasurementBasis.Capture(x).Basis.MethodologyVersion, x.CapturedAt, x.ScoredResponseCount))
            .ToList();
        return new(options, Math.Max(1, page), readings.Count == 20);
    }
    public async Task<Guid> Open(Guid o, Guid u, OpenEvolutionCycleRequest x, CancellationToken c) {
        await Authorize(o, u, c);
        if (!await r.LinksAvailable(o, x.DiagnosticId, x.GovernanceCycleId, c)) throw new ArgumentException("Os vínculos não estão disponíveis nesta organização.");
        if (o == Guid.Empty || u == Guid.Empty) throw new UnauthorizedAccessException("Organização e autor são obrigatórios.");
        if (string.IsNullOrWhiteSpace(x.Title) || x.Title.Trim().Length is < 3 or > 160) throw new ArgumentException("O nome do ciclo deve ter entre 3 e 160 caracteres.");
        if (string.IsNullOrWhiteSpace(x.Summary) || x.Summary.Trim().Length > 1000) throw new ArgumentException("O objetivo do ciclo é obrigatório e aceita até 1.000 caracteres.");
        if (string.IsNullOrWhiteSpace(x.EvidenceSummary) || x.EvidenceSummary.Trim().Length > 2000) throw new ArgumentException("Baseline exige evidência com até 2.000 caracteres.");
        if (x.PeriodEnd < x.PeriodStart) throw new ArgumentException("A data final deve ser posterior à data inicial.");
        if (x.PeriodStart == default) throw new ArgumentException("Informe a data inicial do ciclo.");
        if (x.TargetScore is < 0 or > 100) throw new ArgumentException("A meta deve estar entre 0 e 100.");
        if (x.TargetScore is { } target && decimal.Round(target, 2) != target) throw new ArgumentException("A meta aceita até duas casas decimais.");
        var normalized = x with { Title = x.Title.Trim(), Summary = x.Summary.Trim(), EvidenceSummary = x.EvidenceSummary.Trim() };
        var diagnosticSurvey = x.DiagnosticId is { } diagnosticReference ? await r.DiagnosticSurvey(o, diagnosticReference, c) : null;
        Guid? resultSurvey = null;
        if (x.ResultId is { } resultId) {
            resultSurvey = await r.ResultSurvey(o, resultId, c) ?? throw new ArgumentException("O resultado individual não está disponível nesta organização.");
            if (diagnosticSurvey.HasValue && diagnosticSurvey != resultSurvey) throw new ArgumentException("O resultado individual não pertence à pesquisa informada.");
            normalized = normalized with { DiagnosticId = x.DiagnosticId ?? resultSurvey };
        }
        if (x.ReferenceMode is not null and not ("manual" or "calculated")) throw new ArgumentException("Selecione a modalidade de referência.");
        if (x.ReferenceMode == "calculated" && x.SourceSurveyId is null) throw new ArgumentException("Selecione a pesquisa da medição calculada.");
        if (x.ReferenceMode == "manual" && x.SourceSurveyId.HasValue) throw new ArgumentException("Uma referência manual não pode utilizar uma origem calculada.");
        normalized = normalized with { BaselineSource = "manual_informed", Measurement = null, MethodologyVersion = null, PopulationSignature = null, DimensionSignature = null, ScaleSignature = null, CalculationCriteria = null, BaselineLevel = null };
        if (x.SourceSurveyId is { } selected) {
            var reading = await intelligence.GetEvolutionSurveyAsync(o, selected, c);
            if (reading is null) throw new ArgumentException("Selecione uma medição organizacional disponível para esta organização.");
            if (resultSurvey.HasValue && resultSurvey != reading.SurveyId) throw new ArgumentException("A medição e o resultado individual pertencem a pesquisas diferentes.");
            if (diagnosticSurvey.HasValue && diagnosticSurvey != reading.SurveyId) throw new ArgumentException("A medição selecionada não pertence ao diagnóstico informado.");
            if (!reading.MaturityIndex.HasValue) throw new ArgumentException("A medição selecionada ainda não possui resultado organizacional calculado.");
            if (reading.MaturityIndex is < 0 or > 100) throw new ArgumentException("A medição está fora da escala de 0 a 100.");
            var measurement = EvolutionMeasurementBasis.Capture(reading);
            normalized = normalized with {
                DiagnosticId = x.DiagnosticId ?? reading.SurveyId,
                ResultId = x.ResultId,
                Measurement = measurement,
                BaselineScore = reading.MaturityIndex,
                BaselineLevel = "Medição organizacional",
                MethodologyVersion = measurement.Basis.MethodologyVersion,
                PopulationSignature = measurement.Basis.PopulationSignature,
                DimensionSignature = measurement.Basis.DimensionSignature,
                ScaleSignature = measurement.Basis.ScaleSignature,
                CalculationCriteria = measurement.Basis.CalculationCriteria,
                BaselineSource = "organizational_measurement",
                EvidenceSummary = $"{normalized.EvidenceSummary} Medição inicial selecionada: {reading.Title}, {reading.MaturityIndex:0.0} pontos, {reading.ScoredResponseCount} resposta(s) pontuada(s), capturada em {reading.CapturedAt:dd/MM/yyyy}."
            };
        }
        else if (x.BaselineScore is < 0 or > 100) throw new ArgumentException("Baseline e meta devem estar entre 0 e 100.");
        else if (x.BaselineScore is { } manual && decimal.Round(manual, 2) != manual) throw new ArgumentException("A referência manual aceita até duas casas decimais.");
        if (normalized.EvidenceSummary.Length > 2000) throw new ArgumentException("A evidência contextualizada ultrapassa 2.000 caracteres. Reduza o texto inicial.");
        return await r.Open(o, u, normalized, c);
    }
    public Task Close(Guid o, Guid u, Guid id, string learning, CancellationToken c) => throw new ArgumentException("O encerramento exige versão e tratamento das pendências. Utilize a revisão de encerramento.");

    private async Task Authorize(Guid o, Guid u, CancellationToken c) {
        var access = context.GetCurrent();
        if (access.RequireOrganizationId() != o || access.RequireUserId() != u || (!access.IsGlobalAdministrator && !access.Permissions.Contains(ValoraPermissions.Evolution.Manage, StringComparer.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("Sem permissão para gerenciar a evolução desta organização.");
        var module = await commercial.EvaluateAccessAsync(o, "evolution", true, c);
        if (!module.Allowed || module.ReadOnly) throw new UnauthorizedAccessException(module.Message);
    }
    private void AuthorizeRead(Guid o) {
        var access = context.GetCurrent();
        if (access.RequireOrganizationId() != o || (!access.IsGlobalAdministrator && !access.Permissions.Contains(ValoraPermissions.Evolution.Read, StringComparer.OrdinalIgnoreCase) && !access.Permissions.Contains(ValoraPermissions.Evolution.Manage, StringComparer.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("Sem permissão para consultar a evolução desta organização.");
    }

    public Task<PreservedEvolutionMeasurement?> Baseline(Guid o, Guid cycle, CancellationToken c) { AuthorizeRead(o); return r.Baseline(o, cycle, c); }
    public Task<long> Version(Guid o, Guid cycle, CancellationToken c) { AuthorizeRead(o); return r.Version(o, cycle, c); }

    public async Task<EvolutionReevaluationPreview> Preview(Guid o, Guid u, Guid cycle, Guid surveyId, CancellationToken c) {
        await Authorize(o, u, c);
        var cycleRecord = await r.Get(o, cycle, c) ?? throw new KeyNotFoundException("Ciclo não encontrado.");
        if (cycleRecord.Status is "closed" or "archived") throw new InvalidOperationException("O ciclo está encerrado.");
        var baseline = await r.Baseline(o, cycle, c) ?? throw new InvalidOperationException("A referência inicial não possui uma medição histórica verificável. Revise sua origem antes de comparar.");
        var reading = await intelligence.GetEvolutionSurveyAsync(o, surveyId, c) ?? throw new ArgumentException("A medição não está disponível nesta organização.");
        return EvolutionReevaluation.Preview(await r.Version(o, cycle, c), baseline, EvolutionMeasurementBasis.Capture(reading));
    }

    public async Task<Guid> ConfirmReevaluation(Guid o, Guid u, Guid cycle, Guid surveyId, long version, string fingerprint, string commandId, CancellationToken c) {
        await Authorize(o, u, c);
        if (string.IsNullOrWhiteSpace(commandId) || commandId.Length > 80 || string.IsNullOrWhiteSpace(fingerprint) || fingerprint.Length != 64)
            throw new ArgumentException("A confirmação exige uma prévia válida.");
        var replay = await r.ReevaluationReplay(o, cycle, commandId, fingerprint, surveyId, version, c);
        if (replay.HasValue) return replay.Value;
        var preview = await Preview(o, u, cycle, surveyId, c);
        if (preview.Version != version || !string.Equals(preview.Fingerprint, fingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("O ciclo ou a medição mudou. Revise uma nova prévia antes de confirmar.");
        return await r.Reevaluate(o, u, cycle, preview, commandId, c);
    }

    public async Task CloseReviewed(Guid o, Guid u, Guid cycle, long version, string learning, string pendingTreatment, CancellationToken c) {
        await Authorize(o, u, c);
        if (string.IsNullOrWhiteSpace(learning) || learning.Trim().Length is < 3 or > 2000 || string.IsNullOrWhiteSpace(pendingTreatment) || pendingTreatment.Trim().Length is < 3 or > 2000)
            throw new ArgumentException("Registre a síntese e o tratamento das pendências com 3 a 2.000 caracteres.");
        await r.CloseReviewed(o, u, cycle, version, learning.Trim(), pendingTreatment.Trim(), c);
    }

}
public sealed class EvolutionSnapshotService(IEvolutionRepository r, ICurrentRequestContext context, ICommercialSaasRepository commercial) {
    public Task<IReadOnlyList<EvolutionSnapshotDto>> List(Guid o, Guid id, CancellationToken c) {
        var access = context.GetCurrent();
        if (access.RequireOrganizationId() != o || (!access.IsGlobalAdministrator && !access.Permissions.Contains(ValoraPermissions.Evolution.Read, StringComparer.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("Sem permissão para consultar acompanhamentos.");
        return r.Snapshots(o, id, c);
    }
    public async Task<Guid> Generate(Guid o, Guid u, Guid id, string evidence, string interpretation, string recommendation, CancellationToken c) {
        var access = context.GetCurrent();
        if (access.RequireOrganizationId() != o || access.RequireUserId() != u || (!access.IsGlobalAdministrator && !access.Permissions.Contains(ValoraPermissions.Evolution.SnapshotsGenerate, StringComparer.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("Sem permissão para registrar acompanhamentos.");
        var module = await commercial.EvaluateAccessAsync(o, "evolution", true, c);
        if (!module.Allowed || module.ReadOnly) throw new UnauthorizedAccessException(module.Message);
        if (string.IsNullOrWhiteSpace(evidence) || string.IsNullOrWhiteSpace(interpretation) || string.IsNullOrWhiteSpace(recommendation)) throw new ArgumentException("Evidência, interpretação e próximo passo são obrigatórios.");
        if (evidence.Trim().Length > 2000 || interpretation.Trim().Length > 2000 || recommendation.Trim().Length > 2000) throw new ArgumentException("Evidência, interpretação e recomendação aceitam até 2.000 caracteres.");
        return await r.Snapshot(o, u, id, evidence.Trim(), interpretation.Trim(), recommendation.Trim(), c);
    }
}
public sealed class EvolutionMeasurementService { public static string Trend(decimal? previous, decimal? current, int evidenceCount) { if (evidenceCount < 2 || previous is null || current is null) return "insufficient_sample"; var d = current - previous; return d >= 5 ? "consistent_evolution" : d > 0 ? "gradual_evolution" : d == 0 ? "stability" : d <= -3 ? "regression" : "oscillation"; } }

public sealed record EvolutionComparisonBasis(string MethodologyVersion, string DimensionSignature, string ScaleSignature, string CalculationCriteria, string PopulationSignature, bool DataAvailable);
public sealed record EvolutionComparisonResult(bool IsComparable, decimal? AbsoluteVariation, string Limitation);
public static class EvolutionComparisonService {
    public static EvolutionComparisonResult Compare(decimal? baseline, EvolutionComparisonBasis baselineBasis, decimal? current, EvolutionComparisonBasis currentBasis) {
        if (!baselineBasis.DataAvailable || !currentBasis.DataAvailable || baseline is null || current is null)
            return new(false, null, "Os resultados não estão disponíveis para comparação.");
        var required = new[] {
            ("versão metodológica", baselineBasis.MethodologyVersion, currentBasis.MethodologyVersion),
            ("dimensões", baselineBasis.DimensionSignature, currentBasis.DimensionSignature),
            ("escala", baselineBasis.ScaleSignature, currentBasis.ScaleSignature),
            ("critérios de cálculo", baselineBasis.CalculationCriteria, currentBasis.CalculationCriteria),
            ("população ou recorte", baselineBasis.PopulationSignature, currentBasis.PopulationSignature)
        };
        var missing = required.Where(x => !Known(x.Item2) || !Known(x.Item3)).Select(x => x.Item1).ToList();
        if (missing.Count > 0) return new(false, null, $"Comparação indisponível: faltam informações de {string.Join(", ", missing)}. Os resultados devem ser analisados separadamente.");
        var differences = required.Where(x => !string.Equals(x.Item2.Trim(), x.Item3.Trim(), StringComparison.Ordinal)).Select(x => x.Item1).ToList();
        return differences.Count == 0
            ? new(true, current - baseline, "Variação absoluta observada; não demonstra causalidade do plano.")
            : new(false, null, $"Comparação indisponível: diferem {string.Join(", ", differences)}. Os resultados devem ser analisados separadamente.");
    }
    private static bool Known(string value) => !string.IsNullOrWhiteSpace(value)
        && !string.Equals(value.Trim(), "indefinida", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(value.Trim(), "unknown", StringComparison.OrdinalIgnoreCase);
}
public sealed class OpenEvolutionCycleUseCase(EvolutionCycleService s) { public Task<Guid> Execute(Guid o, Guid u, OpenEvolutionCycleRequest r, CancellationToken c) => s.Open(o, u, r, c); }
public sealed class CloseEvolutionCycleUseCase(EvolutionCycleService s) { public Task Execute(Guid o, Guid u, Guid id, string l, CancellationToken c) => s.Close(o, u, id, l, c); }
public sealed class GenerateEvolutionSnapshotUseCase(EvolutionSnapshotService s) { public Task<Guid> Execute(Guid o, Guid u, Guid id, string e, string i, string r, CancellationToken c) => s.Generate(o, u, id, e, i, r, c); }
