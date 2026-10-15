using System.Text.Json;

namespace Valora.Application.Evolution;

public static class EvolutionPreservedMetadata {
    public static PreservedEvolutionMeasurement? Read(string json, string? property = null) {
        try {
            using var document = JsonDocument.Parse(json);
            var value = document.RootElement;
            if (value.ValueKind != JsonValueKind.Object) return null;
            if (property is not null && !value.TryGetProperty(property, out value)) return null;
            if (value.ValueKind != JsonValueKind.Object
                || !value.TryGetProperty("Score", out var score) || !score.TryGetDecimal(out var number) || number is < 0 or > 100
                || !value.TryGetProperty("ReferenceAt", out _)) return null;
            var measurement = value.Deserialize<PreservedEvolutionMeasurement>();
            return measurement is { Basis: not null, Id: var id, SurveyId: var survey, ReferenceAt: var reference }
                && id != Guid.Empty && survey != Guid.Empty && reference != default
                && !string.IsNullOrWhiteSpace(measurement.Title) ? measurement : null;
        }
        catch (JsonException) { return null; }
        catch (InvalidOperationException) { return null; }
    }
}
