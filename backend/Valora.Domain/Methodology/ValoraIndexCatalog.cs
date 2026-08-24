namespace Valora.Domain.Methodology;

/// <summary>
/// Canonical identifiers for the organizational indices governed by the Valora methodology.
/// Codes are stable integration keys; labels may be localized by presentation layers.
/// </summary>
public static class ValoraIndexCatalog
{
    public static IReadOnlyList<ValoraIndexDefinition> All { get; } =
    [
        new("IMO", "Índice de Maturidade Organizacional"),
        new("ICS", "Índice de Clareza Sistêmica"),
        new("IIO", "Índice de Inteligência Organizacional"),
        new("IGO", "Índice de Governança Organizacional"),
        new("ICO", "Índice de Cultura Organizacional"),
        new("ILI", "Índice de Liderança"),
        new("IPO", "Índice de Pessoas"),
        new("IDO", "Índice de Desenvolvimento Organizacional"),
        new("IAC", "Índice de Accountability"),
        new("IAR", "Índice de Autonomia Responsável"),
        new("IIS", "Índice de Integração Sistêmica"),
        new("ISO", "Índice de Sustentabilidade Organizacional")
    ];

    public static bool IsOfficial(string? code) =>
        !string.IsNullOrWhiteSpace(code) && All.Any(index =>
            string.Equals(index.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
}

public sealed record ValoraIndexDefinition(string Code, string Name);
