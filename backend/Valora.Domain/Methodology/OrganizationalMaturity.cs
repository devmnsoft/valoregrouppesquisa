namespace Valora.Domain.Methodology;

public enum OrganizationalMaturityLevel
{
    Initial,
    Structuring,
    Integrated,
    Mature
}

/// <summary>Official 0–100 maturity classification shared by every delivery channel.</summary>
public static class OrganizationalMaturity
{
    public static OrganizationalMaturityLevel Classify(decimal score)
    {
        if (score is < 0m or > 100m)
            throw new ArgumentOutOfRangeException(nameof(score), score, "The maturity score must be between 0 and 100.");

        return score switch
        {
            <= 25m => OrganizationalMaturityLevel.Initial,
            <= 50m => OrganizationalMaturityLevel.Structuring,
            <= 75m => OrganizationalMaturityLevel.Integrated,
            _ => OrganizationalMaturityLevel.Mature
        };
    }

    public static string Code(decimal score) => Classify(score) switch
    {
        OrganizationalMaturityLevel.Initial => "initial",
        OrganizationalMaturityLevel.Structuring => "structuring",
        OrganizationalMaturityLevel.Integrated => "integrated",
        OrganizationalMaturityLevel.Mature => "mature",
        _ => throw new InvalidOperationException("Unknown maturity classification.")
    };

    public static string Label(decimal score) => Classify(score) switch
    {
        OrganizationalMaturityLevel.Initial => "Inicial",
        OrganizationalMaturityLevel.Structuring => "Estruturante",
        OrganizationalMaturityLevel.Integrated => "Integrado",
        OrganizationalMaturityLevel.Mature => "Maduro",
        _ => throw new InvalidOperationException("Unknown maturity classification.")
    };
}
