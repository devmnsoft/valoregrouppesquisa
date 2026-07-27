namespace Valora.Domain.ValueObjects;

public readonly record struct PlanCode
{
    private static readonly HashSet<string> Supported = ["free", "professional", "corporate", "enterprise", "essential", "growth"];

    public PlanCode(string value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (normalized is null || !Supported.Contains(normalized))
        {
            throw new ArgumentException("Plano inválido.", nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }

    public bool IsLegacy => Value is "essential" or "growth";
}
