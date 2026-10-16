using Microsoft.AspNetCore.Mvc;

namespace Valora.Api.Common;

/// <summary>
/// Métodos de extensão para enriquecimento seguro e idempotente de ProblemDetails,
/// evitando exceções por chaves duplicadas (como traceId adicionado pela factory).
/// </summary>
public static class ProblemDetailsEnrichmentExtensions {
    public static ProblemDetails SetExtension(this ProblemDetails problemDetails, string key, object? value) {
        ArgumentNullException.ThrowIfNull(problemDetails);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        problemDetails.Extensions[key] = value;
        return problemDetails;
    }

    public static ProblemDetails SetExtensions(this ProblemDetails problemDetails, IEnumerable<KeyValuePair<string, object?>> extensions) {
        ArgumentNullException.ThrowIfNull(problemDetails);
        if (extensions is null) return problemDetails;

        foreach (var (key, value) in extensions) {
            if (!string.IsNullOrWhiteSpace(key)) {
                problemDetails.Extensions[key] = value;
            }
        }
        return problemDetails;
    }

    public static ProblemDetails EnsureCorrelation(this ProblemDetails problemDetails, string correlationId) {
        ArgumentNullException.ThrowIfNull(problemDetails);
        if (!string.IsNullOrWhiteSpace(correlationId)) {
            problemDetails.Extensions["correlationId"] = correlationId;
        }
        return problemDetails;
    }
}
