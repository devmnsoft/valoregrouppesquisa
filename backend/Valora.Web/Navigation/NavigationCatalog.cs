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
        Section("executive", "Visão Geral", 10,
            Item("executive.overview", "Visão Geral", "Indicadores e prioridades da organização", "Dashboard", "layout-dashboard", null, null, "dashboard", null, 10, Results),
            ItemAction("executive.cockpit", "Cockpit Executivo", "Riscos, tendências e prioridades da alta gestão", "Experience", "Cockpit", "chart-radar", "results", 15, Results),
            Item("executive.company", "Minha Empresa", "Central operacional da sua conta", "Organization", "building", null, null, "organization", "organization", 20, Administrators)),
        Section("diagnostics", "Diagnósticos", 20,
            ItemAction("diagnostics.new", "Novo Diagnóstico", "Assistente para configurar o próximo ciclo", "Diagnostics", "New", "plus", "forms", 1, Diagnostics),
            ItemAction("diagnostics.cycles", "Ciclos Diagnósticos", "Coleta, processamento e workspace", "Diagnostics", "Index", "activity", "surveys", 3, Diagnostics),
            ItemAction("diagnostics.templates", "Templates", "Biblioteca oficial Valora", "Experience", "Templates", "layers", "forms", 5, Diagnostics),
            Item("diagnostics.forms", "Formulários", "Crie e publique diagnósticos", "Forms", "file-text", null, "forms", "forms", "organization", 10, Diagnostics),
            Item("diagnostics.surveys", "Pesquisas", "Configure campanhas, períodos e distribuição", "Surveys", "file-question", null, "surveys", "surveys", "organization", 20, Diagnostics),
            ItemAction("diagnostics.campaigns", "Campanhas", "Envio, adesão e lembretes", "Experience", "Campaigns", "message-circle", "surveys", 25, Diagnostics),
            Item("diagnostics.responses", "Respostas", "Acompanhe a participação", "Responses", "activity", "canViewResponses", "responses", "responses", "organization", 30, Results)),
        Section("intelligence", "Inteligência", 30,
            Item("intelligence.results", "Resultados", "Devolutivas e análises executivas", "Results", "chart-radar", null, "results", "results", "organization", 10, Results),
            Item("intelligence.organizational", "Inteligência Organizacional", "Dashboard, Heatmap, Evolution, Journey e Action Valora™", "Intelligence", "brain", "organizational_intelligence.read", "organizational_intelligence", "organizational_intelligence", "organization", 15, Results),
            ItemAction("intelligence.dictionary", "Dicionário Cognitivo", "Conceitos oficiais da Metodologia Valora™", "Methodology", "Dictionary", "file-text", "organizational_intelligence", 14, Results),
            ItemAction("intelligence.cognitive-map", "Mapa Cognitivo", "Relações e influências sistêmicas", "Methodology", "CognitiveMap", "brain", "organizational_intelligence", 14, Results),
            ItemAction("intelligence.methodology-mappings", "Mapeamento Metodológico", "Cobertura de perguntas, Metrics e Índices", "Methodology", "Mappings", "layers", "organizational_intelligence", 14, Diagnostics),
            ItemAction("intelligence.processing", "Centro de Processamento", "Fila, etapas, falhas e reprocessamentos", "Intelligence", "Processing", "activity", "organizational_intelligence", 15, Administrators),
            ItemAction("intelligence.evidence", "Evidências", "Origem verificável das leituras", "Intelligence", "Evidence", "activity", "organizational_intelligence", 16, Results),
            ItemAction("intelligence.metrics", "Metrics™", "Indicadores com contexto e evidência", "Intelligence", "Metrics", "chart-radar", "organizational_intelligence", 17, Results),
            ItemAction("intelligence.indices", "Índices Valora™", "Índices oficiais e composição", "Intelligence", "Indices", "layers", "organizational_intelligence", 18, Results),
            ItemAction("intelligence.inference", "Motor de Inferência", "Hipóteses e regras rastreáveis", "Intelligence", "Inference", "brain", "organizational_intelligence", 19, Results),
            ItemAction("intelligence.insights", "Insights IA™", "Leituras sustentadas por evidências", "Intelligence", "Insights", "sparkles", "organizational_intelligence", 20, Results),
            ItemAction("intelligence.radar", "Radar™", "Equilíbrio sistêmico", "Intelligence", "Radar", "chart-radar", "organizational_intelligence", 21, Results),
            ItemAction("intelligence.heatmap", "Heatmap™", "Mapa de atenção", "Intelligence", "Heatmap", "layout-dashboard", "organizational_intelligence", 22, Results),
            ItemAction("intelligence.benchmark", "Benchmark™", "Comparação interna contextual", "Intelligence", "Benchmark", "activity", "organizational_intelligence", 23, Results),
            ItemAction("intelligence.evolution", "Evolution™", "Evolução entre ciclos", "Intelligence", "Evolution", "activity", "organizational_intelligence", 24, Results),
            ItemAction("intelligence.journey", "Journey™", "Memória organizacional", "Intelligence", "Journey", "file-text", "organizational_intelligence", 25, Results),
            ItemAction("intelligence.executive-report", "Executive Reports™", "Preview executivo rastreável", "Intelligence", "ExecutiveReport", "file-text", "organizational_intelligence", 26, Results),
            ItemAction("intelligence.one-on-one", "One-on-One™", "Conversas e compromissos responsáveis", "Intelligence", "OneOnOne", "users", "organizational_intelligence", 27, Results),
            ItemAction("intelligence.comparisons", "Comparativos", "Evolução entre ciclos e áreas", "OperationalIntelligence", "Comparisons", "activity", "results", 20, Results),
            ItemAction("intelligence.recommendations", "Recomendações", "Prioridades orientadas por evidências", "OperationalIntelligence", "Recommendations", "sparkles", "results", 30, Results),
            Item("intelligence.actions", "Plano de Ação", "Kanban de melhoria contínua", "ActionPlans", "layers", "organizational_intelligence.read", "organizational_intelligence", "organizational_intelligence", "organization", 40, Results),
            Item("intelligence.reports", "Relatórios", "Central de geração e downloads", "Reports", "file-text", null, null, "reports", "organization", 50, Results),
            Item("intelligence.certificates", "Certificados", "Emissão e reimpressão", "Certificates", "award", null, "certificates", "certificates", "organization", 60, Results)),
        Section("structure", "Gestão", 40,
            Item("structure.organization", "Dados da Organização", "Perfil, privacidade, marca e assinatura", "Organization", "building", null, null, "organization", "organization", 10, Administrators),
            ItemAction("structure.tree", "Estrutura Organizacional", "Unidades, áreas, departamentos e lideranças", "Organization", "Structure", "layers", "organization", 15, Administrators),
            ItemAction("structure.audiences", "Públicos e Segmentações", "Privacidade e públicos dos diagnósticos", "Diagnostics", "Audiences", "users", "surveys", 20, Diagnostics)),
        Section("administration", "Operação", 50,
            ItemAction("operation.support", "Suporte", "Chamados, comentários e resoluções", "AssistedOperations", "Support", "message-circle", "organization", 1, Administrators),
            ItemAction("operation.feedback", "Feedback", "Experiência e melhorias solicitadas", "AssistedOperations", "Feedback", "sparkles", "organization", 2, Administrators),
            ItemAction("operation.customer-success", "Customer Success", "Saúde e riscos da carteira", "AssistedOperations", "CustomerSuccess", "users", "organization", 3, Administrators),
            ItemAction("operation.usage", "Métricas de Adoção", "Uso real por organização", "AssistedOperations", "Usage", "chart-radar", "organization", 4, Administrators),
            ItemAction("operation.onboarding", "Onboarding", "Checklist operacional pós-venda", "AssistedOperations", "Onboarding", "file-text", "organization", 5, Administrators),
            ItemAction("operation.upgrades", "Solicitações Comerciais", "Upgrade, limites e consultoria", "AssistedOperations", "Upgrades", "activity", "organization", 6, Administrators),
            ItemAction("operation.incidents", "Incidentes", "Investigação e mitigação", "AssistedOperations", "Incidents", "activity", "organization", 7, Administrators),
            ItemAction("operation.releases", "Release Notes", "Evolução controlada do produto", "AssistedOperations", "Releases", "layers", "organization", 8, Administrators),
            ItemAction("operation.data-quality", "Qualidade dos Dados", "Verificações seguras e não destrutivas", "AssistedOperations", "DataQuality", "layout-dashboard", "organization", 9, Administrators),
            Item("administration.plans", "Planos e Uso", "Recursos, limites e consumo atual", "Plans", "activity", null, null, "plans", "organization", 5, Administrators),
            Item("administration.access", "Central de Acessos", "Usuários, papéis e escopos", "Users", "users", null, null, "users", "organization", 10, Administrators),
            Item("administration.communications", "Comunicações", "Templates e entregas", "Communications", "message-circle", null, null, "communications", "organization", 20, Administrators),
            Item("administration.audit", "Auditoria", "Histórico de atividades", "Audit", "activity", null, null, "audit", "organization", 30, Roles("admin_valora", "consultor_valora")),
            ItemAction("administration.integrations", "Integrações", "Power BI™ e exportações autorizadas", "Intelligence", "Integrations", "layers", "organizational_intelligence", 35, Administrators),
            ItemAction("administration.platform-governance", "Governança da Plataforma", "Rastreabilidade, integridade e controle", "Intelligence", "PlatformGovernance", "activity", "organizational_intelligence", 36, Administrators),
            Item("administration.health", "Saúde do Sistema", "API, banco e configurações operacionais", "EnvironmentStatus", "activity", null, null, "settings", "organization", 38, Administrators),
            Item("administration.settings", "Configurações", "Preferências da plataforma", "Settings", "settings", null, null, "settings", "organization", 40, Administrators)),
        Section("support", "Suporte", 60,
            ItemAction("support.help", "Ajuda", "Guias rápidos e atendimento", "Experience", "Help", "file-question", "organization", 10, Results))
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
