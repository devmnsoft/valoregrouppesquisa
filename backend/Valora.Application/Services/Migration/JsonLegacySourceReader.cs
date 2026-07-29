using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public abstract class JsonLegacySourceReader(
    ILegacyMappingService mapping,
    ILegacyDataNormalizer normalizer) : ILegacySourceReader
{
    public abstract bool CanRead(string sourceType);

    public Task<LegacySourceReadResult> ReadAsync(MigrationUploadRequest request, CancellationToken ct = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(request.PayloadJson);
            var list = new List<LegacySourceDocument>();
            ReadRoot(doc.RootElement, list);

            var sha = Convert
                .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.PayloadJson)))
                .ToLowerInvariant();

            return Task.FromResult(new LegacySourceReadResult(request.SourceType, request.SourceName, sha, list));
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON inválido para importação: {ex.Message}");
        }
    }

    private void ReadRoot(JsonElement root, List<LegacySourceDocument> list)
    {
        var source = root.TryGetProperty("collections", out var c)
            ? c
            : root.TryGetProperty("data", out var d)
                ? d
                : root;

        foreach (var col in source.EnumerateObject())
        {
            if (col.Value.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var item in col.Value.EnumerateArray())
                {
                    Add(col.Name, Id(item) ?? (++i).ToString(), item, list);
                }
            }
            else if (col.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var item in col.Value.EnumerateObject())
                {
                    Add(col.Name, item.Name, item.Value, list);
                }
            }
        }
    }

    private static string? Id(JsonElement e) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty("id", out var id) ? id.ToString() : null;

    private void Add(string collection, string id, JsonElement item, List<LegacySourceDocument> list)
    {
        var raw = item.GetRawText();
        var fields = item.ValueKind == JsonValueKind.Object
            ? item.EnumerateObject().Select(p => p.Name)
            : Array.Empty<string>();
        var sensitive = fields
            .Where(f => Regex.IsMatch(f, "(?i)(password|senha|token|secret|hash|smtp|connection|refresh|string)"))
            .ToArray();
        var maskedRaw = normalizer.MaskSensitiveJson(raw);

        list.Add(new LegacySourceDocument(
            collection,
            id,
            mapping.MapCollectionToTarget(collection),
            maskedRaw,
            maskedRaw,
            mapping.GetUnmappedFields(collection, fields),
            sensitive));
    }
}
