namespace Valora.Application.Common;

/// <summary>
/// Stable feature identifiers used by plan and entitlement checks.
/// </summary>
public static class FeatureCodes
{
    // This is the module code currently persisted for the Enterprise integrations bundle.
    public const string EnterpriseIntegrations = "enterprise_integrations";

    public const string EnterpriseApiKeys = "enterprise.api_keys";
    public const string EnterpriseWebhooks = "enterprise.webhooks";
    public const string EnterprisePowerBi = "enterprise.powerbi";
    public const string EnterpriseOneOnOne = "enterprise.one_on_one";
    public const string EnterpriseBenchmarkAdvanced = "enterprise.benchmark_advanced";
    public const string EnterpriseImports = "enterprise.imports";
    public const string EnterprisePublicApi = "enterprise.public_api";
    public const string EnterpriseMultiUnitComparison = "enterprise.multi_unit_comparison";
    public const string EnterpriseConsultantAccess = "enterprise.consultant_access";

    public const string ProfessionalIntelligence = "professional.intelligence";
    public const string ProfessionalMetrics = "professional.metrics";
    public const string ProfessionalIndices = "professional.indices";
    public const string ProfessionalInsights = "professional.insights";
    public const string ProfessionalAction = "professional.action";
    public const string ProfessionalEvolution = "professional.evolution";
    public const string ProfessionalJourney = "professional.journey";
    public const string ProfessionalExecutiveReport = "professional.executive_report";
    public const string ProfessionalCertificates = "professional.certificates";

    public const string FreeBasicDiagnostic = "free.basic_diagnostic";
    public const string FreeBasicResult = "free.basic_result";
}
