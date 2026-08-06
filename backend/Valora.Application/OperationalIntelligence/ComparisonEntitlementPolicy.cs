namespace Valora.Application.OperationalIntelligence;

public enum ComparisonScope { CompanyBenchmark, Department, Unit, MultiOrganization }
public sealed class ComparisonEntitlementPolicy
{
    public bool CanAccess(string? planName, ComparisonScope scope)
    {
        var plan = (planName ?? "gratuito").Trim().ToLowerInvariant();
        return scope switch
        {
            ComparisonScope.Department => plan is "profissional" or "corporativo" or "enterprise",
            ComparisonScope.Unit => plan is "corporativo" or "enterprise",
            ComparisonScope.MultiOrganization => plan is "enterprise",
            _ => plan is not "gratuito"
        };
    }
}
