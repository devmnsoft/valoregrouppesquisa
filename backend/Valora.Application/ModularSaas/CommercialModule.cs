namespace Valora.Application.ModularSaas;

public sealed record CommercialModule(
    Guid Id,
    string Code,
    string Name,
    string Description,
    decimal BasePrice,
    string Status,
    string Icon,
    string MainRoute,
    string MenuCategory,
    bool RequiresContract,
    string AccessModuleCode,
    int DisplayOrder,
    string ContractStatus,
    bool IsContracted,
    bool IsReadOnly,
    int FeatureCount);
