namespace Valora.Application.Results;

/// <summary>Classifica robustez sem converter volume de respostas em certeza causal.</summary>
public static class EvidenceConfidence
{
    public static string Classify(int convergentEvidenceCount) => convergentEvidenceCount switch
    {
        >= 6 => "muito alta",
        >= 4 => "alta",
        3 => "moderada",
        _ => "baixa"
    };

    public static bool AllowsConclusion(int convergentEvidenceCount) => convergentEvidenceCount >= 3;
}
