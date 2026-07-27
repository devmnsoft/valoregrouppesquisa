namespace Valora.Domain.ValueObjects;

public readonly record struct PhoneNumber
{
    public PhoneNumber(string value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        if (digits.Length is < 10 or > 15)
        {
            throw new ArgumentException("Telefone inválido.", nameof(value));
        }

        Value = digits;
    }

    public string Value { get; }
}
