using System.Text.RegularExpressions;

namespace Valora.Domain.ValueObjects;

public readonly partial record struct OrganizationSlug
{
    public OrganizationSlug(string value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length is < 3 or > 80 || !SlugPattern().IsMatch(normalized))
        {
            throw new ArgumentException("Slug inválido.", nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
