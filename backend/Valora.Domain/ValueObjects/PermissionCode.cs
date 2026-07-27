using System.Text.RegularExpressions;

namespace Valora.Domain.ValueObjects;

public readonly partial record struct PermissionCode
{
    public PermissionCode(string value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !PermissionPattern().IsMatch(normalized))
        {
            throw new ArgumentException("Código de permissão inválido.", nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }

    [GeneratedRegex("^[a-z][a-z0-9_]*\\.[a-z][a-z0-9_]*$")]
    private static partial Regex PermissionPattern();
}
