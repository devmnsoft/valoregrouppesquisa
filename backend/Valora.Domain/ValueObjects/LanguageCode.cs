namespace Valora.Domain.ValueObjects;

public readonly record struct LanguageCode
{
    private static readonly HashSet<string> Supported = ["pt-BR", "en-US", "es-ES", "zh-CN"];

    public LanguageCode(string value)
    {
        var normalized = Supported.FirstOrDefault(code => string.Equals(code, value?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (normalized is null)
        {
            throw new ArgumentException("Idioma não suportado.", nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }
}
