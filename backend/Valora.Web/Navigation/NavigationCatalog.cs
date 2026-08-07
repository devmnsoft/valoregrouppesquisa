namespace Valora.Web.Navigation;

public sealed class NavigationCatalog
{
    private static readonly IReadOnlySet<string> Administrators = Roles("admin_valora", "consultor_valora", "empresa_admin");
    private static readonly IReadOnlySet<string> Diagnostics = Roles("admin_valora", "consultor_valora", "empresa_admin", "gestor_pesquisa");
    private static readonly IReadOnlySet<string> Results = Roles("admin_valora", "consultor_valora", "empresa_admin", "gestor_pesquisa", "analista_resultados", "gestor_area");

    public IReadOnlyList<NavigationSection> Sections { get; } =
    [
        Section("valora", "Admin Valora", 5,
            Item("valora.overview", "Visão Geral Valora", "Carteira, CRM e operação SaaS", "Enterprise", "sparkles", null, null, "enterprise", null, 10, Roles("admin_valora"))),
        Section("executive", "Visão Executiva", 10,
            Item("executive.overview", "Visão Geral", "Indicadores e prioridades da organização", "Dashboard", "layout-dashboard", null, null, "dashboard", null, 10, Results),
            Item("executive.company", "Minha Empresa", "Central operacional da sua conta", "Organization", "building", null, null, "organization", "organization", 20, Administrators)),
        Section("diagnostics", "Diagnósticos", 20,
            Item("diagnostics.forms", "Formulários", "Crie e publique diagnósticos", "Forms", "file-text", null, "forms", "forms", "organization", 10, Diagnostics),
            Item("diagnostics.surveys", "Pesquisas", "Configure campanhas, períodos e distribuição", "Surveys", "file-question", null, "surveys", "surveys", "organization", 20, Diagnostics),
            Item("diagnostics.responses", "Respostas", "Acompanhe a participação", "Responses", "activity", "canViewResponses", "responses", "responses", "organization", 30, Results)),
        Section("intelligence", "Inteligência", 30,
            Item("intelligence.results", "Resultados", "Devolutivas e análises executivas", "Results", "chart-radar", null, "results", "results", "organization", 10, Results),
            ItemAction("intelligence.comparisons", "Comparativos", "Evolução entre ciclos e áreas", "OperationalIntelligence", "Comparisons", "activity", "results", 20, Results),
            ItemAction("intelligence.recommendations", "Recomendações", "Prioridades orientadas por evidências", "OperationalIntelligence", "Recommendations", "sparkles", "results", 30, Results),
            ItemAction("intelligence.actions", "Plano de Ação", "Kanban de melhoria contínua", "OperationalIntelligence", "ActionPlans", "layers", "results", 40, Results),
            Item("intelligence.reports", "Relatórios", "Central de geração e downloads", "Reports", "file-text", null, null, "reports", "organization", 50, Results),
            Item("intelligence.certificates", "Certificados", "Emissão e reimpressão", "Certificates", "award", null, "certificates", "certificates", "organization", 60, Results)),
        Section("structure", "Estrutura", 40,
            Item("structure.organization", "Organização", "Estrutura, marca e assinatura", "Organization", "building", null, null, "organization", "organization", 10, Administrators)),
        Section("administration", "Administração", 50,
            Item("administration.access", "Central de Acessos", "Usuários, papéis e escopos", "Users", "users", null, null, "users", "organization", 10, Administrators),
            Item("administration.communications", "Comunicações", "Templates e entregas", "Communications", "message-circle", null, null, "communications", "organization", 20, Administrators),
            Item("administration.audit", "Auditoria", "Histórico de atividades", "Audit", "activity", null, null, "audit", "organization", 30, Roles("admin_valora", "consultor_valora")),
            Item("administration.settings", "Configurações", "Preferências da plataforma", "Settings", "settings", null, null, "settings", "organization", 40, Administrators))
    ];

    private static NavigationSection Section(string code, string label, int order, params NavigationItem[] items) => new(code, label, order, items);

    private static NavigationItem Item(string code, string label, string description, string controller, string icon,
        string? permission, string? capability, string moduleCode, string? scope, int order, IReadOnlySet<string> roles) =>
        new(code, label, description, NavigationDestination.Mvc(controller), icon, permission, capability, moduleCode, scope, order, null, roles);

    private static NavigationItem ItemAction(string code, string label, string description, string controller, string action,
        string icon, string moduleCode, int order, IReadOnlySet<string> roles) =>
        new(code, label, description, NavigationDestination.Mvc(controller, action), icon, null, null, moduleCode, "organization", order, null, roles);

    private static IReadOnlySet<string> Roles(params string[] values) => new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
}
