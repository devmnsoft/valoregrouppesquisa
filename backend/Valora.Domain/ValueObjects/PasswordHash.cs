namespace Valora.Domain.ValueObjects;

public readonly record struct PasswordHash
{
    public PasswordHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 20)
        {
            throw new ArgumentException("Hash de senha inválido.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }
}
