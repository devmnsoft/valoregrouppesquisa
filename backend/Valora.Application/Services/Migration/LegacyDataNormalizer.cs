using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public sealed class LegacyDataNormalizer : ILegacyDataNormalizer
{
    private static readonly Regex Sensitive = new(
        "(?i)(password|senha|token|secret|smtp|connection|string|hash|refresh)");

    public string? NormalizeEmail(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    public string? NormalizeDocument(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Regex.Replace(value, "\\D", "");

    public string NormalizeStatus(string? value) => (value ?? "active").Trim().ToLowerInvariant() switch
    {
        "ativo" or "active" or "published" => "active",
        "inativo" or "inactive" or "disabled" => "inactive",
        "draft" or "rascunho" => "draft",
        "completed" or "concluido" => "completed",
        _ => "pending"
    };

    public string NormalizeRole(string? value) => (value ?? "participant").Trim().ToLowerInvariant() switch
    {
        "admin" or "admin_valora" or "superadmin" => "admin_valora",
        "gestor" or "manager" => "manager",
        _ => "participant"
    };

    public string NormalizeModule(string? value) =>
        Regex.Replace((value ?? "core").Trim().ToLowerInvariant(), "[^a-z0-9_-]", "-");

    public DateTime? NormalizeDate(object? value) =>
        value is null
            ? null
            : DateTime.TryParse(value.ToString(), out var dt)
                ? DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToUniversalTime()
                : null;

    public string MaskSensitiveJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "{}";
        }

        using var doc = JsonDocument.Parse(json);
        return Mask(doc.RootElement).GetRawText();
    }

    private JsonElement Mask(JsonElement element)
    {
        object? maskedValue = element.ValueKind switch
        {
            JsonValueKind.Object => element
                .EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => Sensitive.IsMatch(property.Name)
                        ? (object)"***MASKED***"
                        : Mask(property.Value)),

            JsonValueKind.Array => element
                .EnumerateArray()
                .Select(item => (object)Mask(item))
                .ToArray(),

            JsonValueKind.String => element.GetString() ?? string.Empty,

            JsonValueKind.Number => element.TryGetDecimal(out var number)
                ? number
                : element.GetRawText(),

            JsonValueKind.True => true,

            JsonValueKind.False => false,

            JsonValueKind.Null => null,

            _ => element.GetRawText()
        };

        return JsonSerializer.SerializeToElement<object?>(maskedValue);
    }
}
