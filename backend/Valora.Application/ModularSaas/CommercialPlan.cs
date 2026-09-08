namespace Valora.Application.ModularSaas;

public sealed record CommercialPlan(
    Guid Id,
    string Code,
    string Name,
    string Description,
    decimal MonthlyPrice,
    decimal AnnualPrice,
    int ModuleCount,
    int DisplayOrder);
