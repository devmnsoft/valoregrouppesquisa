using Valora.Application.ModularSaas;

namespace Valora.Web.Models.ViewModels;

public sealed record SaasMarketplaceViewModel(
    IReadOnlyList<CommercialModule> Modules,
    IReadOnlyList<CommercialPlan> Plans,
    bool IsPlatformAdministrator,
    Guid? ClientId,
    string? BlockedModule = null);
