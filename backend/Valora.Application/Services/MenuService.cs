using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class MenuService(IEntitlementService entitlements, IPermissionService permissions) : IMenuService
{
    public async Task<IReadOnlyList<MenuItemDto>> GetMenuAsync(Guid userId, Guid? organizationId)
    {
        var modules = organizationId.HasValue
            ? (await entitlements.ResolveAsync(organizationId.Value)).EnabledModules
            : Array.Empty<string>();
        bool Has(params string[] aliases) => aliases.Any(alias => modules.Contains(alias, StringComparer.OrdinalIgnoreCase));

        var items = new List<MenuItemDto>
        {
            new("dashboard", "Dashboard", "/Dashboard", "speedometer", 10, Array.Empty<MenuItemDto>()),
            new("surveys", "Pesquisas", "/Surveys", "clipboard", 20, Array.Empty<MenuItemDto>())
        };
        if (!organizationId.HasValue || Has("organizational_intelligence", "inteligenciaOrganizacional"))
            items.Add(new("organizational_intelligence", "Inteligência Organizacional", "/Recomendacoes", "chart-radar", 25, Array.Empty<MenuItemDto>()));
        if (!organizationId.HasValue || Has("relatorios")) items.Add(new("reports", "Relatórios", "/Reports", "bar-chart", 30, Array.Empty<MenuItemDto>()));
        if (!organizationId.HasValue || Has("certificados")) items.Add(new("certificates", "Certificados", "/Certificates", "award", 40, Array.Empty<MenuItemDto>()));
        if (!organizationId.HasValue || Has("exportacoes")) items.Add(new("exports", "Exportações", "/Exports", "download", 50, Array.Empty<MenuItemDto>()));
        if (!organizationId.HasValue || Has("lgpd")) items.Add(new("lgpd", "LGPD", "/Lgpd", "shield", 60, Array.Empty<MenuItemDto>()));
        if (!organizationId.HasValue || Has("convites_email")) items.Add(new("email", "E-mail", "/Email", "mail", 70, Array.Empty<MenuItemDto>()));
        if (await permissions.HasPermissionAsync(userId, "audit.read", organizationId)) items.Add(new("audit", "Auditoria", "/Audit", "activity", 80, Array.Empty<MenuItemDto>()));
        return items;
    }
}
