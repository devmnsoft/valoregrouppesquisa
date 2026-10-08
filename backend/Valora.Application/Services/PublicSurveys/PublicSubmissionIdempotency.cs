using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

/// <summary>
/// Deterministic key and content-hash for public survey submission idempotency.
/// The key namespaces the shared idempotency_keys table by survey, public token
/// and client key; the hash binds the key to the exact request payload so a
/// reused key with different content is rejected instead of silently replayed.
/// </summary>
public static class PublicSubmissionIdempotency {
    public static string BuildKey(Guid surveyId, string? token, string? clientKey) {
        var tokenHash = Hash(token ?? string.Empty);
        var safeClientKey = string.IsNullOrWhiteSpace(clientKey) ? string.Empty : clientKey.Trim();
        return $"pubres:{surveyId:N}:{tokenHash}:{safeClientKey}";
    }

    public static string ComputeRequestHash(SubmitSurveyResponseRequest request) {
        var json = JsonSerializer.Serialize(request, JsonSerializerOptions.Web);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
