using Valora.Application.ModularSaas;
using Valora.Application.SaasAdministration;

namespace Valora.Web.Models.ViewModels;

public sealed record SaasClientModulesViewModel(SaasCustomerDto Client, IReadOnlyList<CommercialModule> Modules);
