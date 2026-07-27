namespace Valora.Domain.ValueObjects;

public readonly record struct RefreshTokenHash
{
    public RefreshTokenHash(string value)
    {
        if (value?.Length != 64 || value.Any(character => !char.IsAsciiHexDigit(character)))
        {
            throw new ArgumentException("Hash de refresh token inválido.", nameof(value));
        }

        Value = value.ToLowerInvariant();
    }

    public string Value { get; }
}
