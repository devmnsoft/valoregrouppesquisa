using System.Globalization;
using System.Text.Json;
using Valora.Application.OrganizationalIntelligence;

namespace Valora.Application.Evolution;

public sealed record PreservedEvolutionMeasurement(Guid Id, Guid SurveyId, string Title, decimal Score,
    DateTime ReferenceAt, int ResponseCount, EvolutionComparisonBasis Basis, string SourceJson,
    string Limitation = "Leitura agregada das respostas pontuadas; não demonstra causalidade.");

public static class EvolutionMeasurementBasis {
    public static PreservedEvolutionMeasurement Capture(EvolutionSurveyComparisonDto reading) {
        if (reading.MaturityIndex is null) throw new ArgumentException("A pesquisa ainda não possui medição calculada.");
        if (reading.MaturityIndex is < 0 or > 100) throw new ArgumentException("A medição está fora da escala de 0 a 100.");
        try { return CaptureCore(reading); }
        catch (JsonException) {
            return new(Guid.NewGuid(), reading.SurveyId, reading.Title,
                reading.MaturityIndex ?? throw new ArgumentException("A pesquisa ainda não possui medição calculada."),
                reading.CapturedAt, reading.ScoredResponseCount,
                new("", "", "", "", reading.PopulationSignature, false), JsonSerializer.Serialize(reading),
                "Os metadados preservados desta medição são inválidos. A comparação está indisponível; solicite revisão da origem. O resultado original foi preservado.");
        }
    }

    private static PreservedEvolutionMeasurement CaptureCore(EvolutionSurveyComparisonDto reading) {
        if (reading.MaturityIndex is null) throw new ArgumentException("A pesquisa ainda não possui medição calculada.");
        if (reading.MaturityIndex is < 0 or > 100) throw new ArgumentException("A medição está fora da escala de 0 a 100.");
        var dimensions = reading.MeasuredDimensionCodes.Select(x => x.Trim()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var scales = new List<string>();
        var scaleDimensions = new HashSet<string>(StringComparer.Ordinal);
        var methodology = string.Empty;
        using var doc = JsonDocument.Parse(reading.SnapshotJson);
        if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("methodology", out var method)
            && method.ValueKind == JsonValueKind.Object && method.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(code.GetString()) && method.TryGetProperty("version", out var version)
            && version.ValueKind is JsonValueKind.String or JsonValueKind.Number && !string.IsNullOrWhiteSpace(version.ToString()))
            methodology = $"{code.GetString()!.Trim()}@{(version.ValueKind == JsonValueKind.Number ? CanonicalJson(version) : version.ToString().Trim())}";
        if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("questions", out var questions) && questions.ValueKind == JsonValueKind.Array)
            foreach (var question in questions.EnumerateArray())
                if (question.ValueKind == JsonValueKind.Object && question.TryGetProperty("dimension", out var dimension) && dimension.ValueKind == JsonValueKind.String && dimensions.Contains(dimension.GetString()?.Trim())
                    && question.TryGetProperty("scale", out var scale) && scale.ValueKind == JsonValueKind.Object && scale.EnumerateObject().Any()) {
                    scales.Add($"{dimension.GetString()?.Trim()}:{CanonicalJson(scale)}");
                    scaleDimensions.Add(dimension.GetString()!.Trim());
                }
        var criteria = reading.CriteriaRows.Where(x => dimensions.Contains(x.DimensionCode?.Trim())).Select(x =>
            $"{x.DimensionCode?.Trim()}|{x.MetricCode?.Trim()}|{x.IndexCode?.Trim()}|{NormalizeNumber(x.WeightText)}|{x.Polarity.ToString(CultureInfo.InvariantCulture)}|{NormalizeRule(x.CalculationRuleJson)}").Order(StringComparer.Ordinal);
        var calculation = string.Join(';', criteria);
        foreach (var property in new[] { "scoringRules", "indices" })
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty(property, out var rules) && rules.ValueKind == JsonValueKind.Array && rules.GetArrayLength() > 0)
                calculation += $"|{property}:" + string.Join(';', rules.EnumerateArray().Select(CanonicalJson).Order(StringComparer.Ordinal));
        if (dimensions.Any(dimension => !reading.CriteriaRows.Any(row => row.DimensionCode?.Trim() == dimension)))
            calculation = string.Empty;
        return new(Guid.NewGuid(), reading.SurveyId, reading.Title, reading.MaturityIndex.Value, reading.CapturedAt, reading.ScoredResponseCount,
            new(methodology, string.Join('|', dimensions), dimensions.All(scaleDimensions.Contains) ? string.Join('|', scales.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)) : string.Empty,
                calculation, reading.PopulationSignature.Trim(), true), JsonSerializer.Serialize(reading),
                $"Leitura agregada de {reading.ScoredResponseCount} resposta(s) pontuada(s). Não demonstra causalidade. Não informa representatividade estatística do recorte.");
    }

    private static string NormalizeNumber(string value) => decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
        ? number.ToString("G29", CultureInfo.InvariantCulture) : value.Trim();
    private static string NormalizeRule(string value) { using var doc = JsonDocument.Parse(value); return CanonicalJson(doc.RootElement); }

    private static string CanonicalJson(JsonElement element) => element.ValueKind switch {
        JsonValueKind.Object => "{" + string.Join(',', element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal).Select(x => JsonSerializer.Serialize(x.Name) + ":" + CanonicalJson(x.Value))) + "}",
        JsonValueKind.Array => "[" + string.Join(',', element.EnumerateArray().Select(CanonicalJson)) + "]",
        JsonValueKind.Number => element.TryGetDecimal(out var number) ? number.ToString("G29", CultureInfo.InvariantCulture) : element.GetRawText(),
        JsonValueKind.String => JsonSerializer.Serialize(element.GetString()?.Trim()),
        _ => element.GetRawText()
    };
}
