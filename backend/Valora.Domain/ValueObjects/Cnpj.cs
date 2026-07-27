using System.Text.RegularExpressions;

namespace Valora.Domain.ValueObjects;

/// <summary>Validated, normalized Brazilian company registration number.</summary>
public sealed partial class Cnpj : IEquatable<Cnpj>
{
    public string Value { get; }
    public string Root => Value[..8];
    public string Formatted => $"{Value[..2]}.{Value[2..5]}.{Value[5..8]}/{Value[8..12]}-{Value[12..]}";
    public string Masked => $"**.{Value[2..5]}.{Value[5..8]}/{Value[8..12]}-**";

    private Cnpj(string value) => Value = value;

    public static Cnpj Create(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        var digits = NonDigitRegex().Replace(input, string.Empty);
        if (digits.Length != 14 || digits.Distinct().Count() == 1 || !HasValidCheckDigits(digits))
            throw new ArgumentException("CNPJ inválido.", nameof(input));
        return new Cnpj(digits);
    }

    public static bool TryCreate(string? input, out Cnpj? cnpj)
    {
        try { cnpj = Create(input ?? string.Empty); return true; }
        catch (ArgumentException) { cnpj = null; return false; }
    }

    private static bool HasValidCheckDigits(string value)
    {
        ReadOnlySpan<int> firstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        ReadOnlySpan<int> secondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        return Digit(value.AsSpan(0, 12), firstWeights) == value[12] - '0'
            && Digit(value.AsSpan(0, 13), secondWeights) == value[13] - '0';
    }

    private static int Digit(ReadOnlySpan<char> digits, ReadOnlySpan<int> weights)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++) sum += (digits[i] - '0') * weights[i];
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    public bool Equals(Cnpj? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Cnpj other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Formatted;

    [GeneratedRegex("\\D", RegexOptions.CultureInvariant)]
    private static partial Regex NonDigitRegex();
}
