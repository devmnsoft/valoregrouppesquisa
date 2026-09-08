using Valora.Application.ModularSaas;
using Valora.Application.SaasAdministration;

namespace Valora.Web.Models.ViewModels;

public sealed record SaasAdminDashboardViewModel(
    IReadOnlyList<SaasCustomerDto> Customers,
    IReadOnlyList<CommercialModule> Modules,
    IReadOnlyList<CommercialPlan> Plans);
