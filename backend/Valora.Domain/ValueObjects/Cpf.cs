using System.Text.RegularExpressions;

namespace Valora.Domain.ValueObjects;

public sealed partial class Cpf : IEquatable<Cpf>
{
    public string Value { get; }
    public string Formatted => $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..]}";
    public string Masked => $"***.{Value[3..6]}.{Value[6..9]}-**";

    private Cpf(string value) => Value = value;

    public static Cpf Create(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        var digits = NonDigitRegex().Replace(input, string.Empty);
        if (digits.Length != 11 || digits.Distinct().Count() == 1 || !HasValidCheckDigits(digits))
            throw new ArgumentException("CPF inválido.", nameof(input));
        return new Cpf(digits);
    }

    public static bool TryCreate(string? input, out Cpf? cpf)
    {
        try { cpf = Create(input ?? string.Empty); return true; }
        catch (ArgumentException) { cpf = null; return false; }
    }

    private static bool HasValidCheckDigits(string value)
    {
        var firstSum = 0;
        for (var index = 0; index < 9; index++) firstSum += (value[index] - '0') * (10 - index);
        var first = firstSum % 11 is 0 or 1 ? 0 : 11 - firstSum % 11;
        if (first != value[9] - '0') return false;
        var secondSum = 0;
        for (var index = 0; index < 10; index++) secondSum += (value[index] - '0') * (11 - index);
        var second = secondSum % 11 is 0 or 1 ? 0 : 11 - secondSum % 11;
        return second == value[10] - '0';
    }

    public bool Equals(Cpf? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Cpf other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Formatted;

    [GeneratedRegex("\\D", RegexOptions.CultureInvariant)]
    private static partial Regex NonDigitRegex();
}
