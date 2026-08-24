using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

/// <summary>Stable entry points for the platform administration console.</summary>
[Authorize(Roles = "admin_valora")]
public sealed class AdministrationController : Controller
{
    private static readonly IReadOnlyDictionary<string, (string Title, string Subtitle, string Endpoint, string Permission)> Modules =
        new Dictionary<string, (string, string, string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["organizations"] = ("Organizações", "Clientes, planos, consumo e situação operacional.", "/bff/enterprise/organizations", "organizations.read"),
            ["users"] = ("Usuários", "Contas, vínculos, perfis e acessos efetivos.", "/bff/users", "users.read"),
            ["roles"] = ("Perfis e Permissões", "Papéis e permissões agrupadas pelo catálogo canônico.", "/bff/roles", "roles.read"),
            ["plans"] = ("Planos e Assinaturas", "Planos, limites, consumo e assinaturas das organizações.", "/bff/plans", "plans.read"),
            ["feature-flags"] = ("Feature Flags", "Recursos habilitados por plano e organização.", "/bff/admin/feature-flags", "settings.read"),
            ["diagnostics"] = ("Diagnósticos", "Ciclos, publicação, metodologia e versionamento.", "/bff/surveys", "surveys.read"),
            ["questions"] = ("Questionários e Perguntas", "Templates oficiais, dimensões, conceitos e perguntas versionadas.", "/bff/methodology/questions", "questions.read"),
            ["responses"] = ("Respostas", "Participação, integridade, anonimização e exportações.", "/bff/responses", "responses.read"),
            ["results"] = ("Resultados", "Devolutivas geradas e links públicos rastreáveis.", "/bff/admin/results", "results.read"),
            ["reports"] = ("Relatórios", "Geração, downloads e falhas de relatórios executivos.", "/bff/reports", "reports.read"),
            ["certificates"] = ("Certificados", "Emissão, download, revogação e validação.", "/bff/admin/certificates", "certificates.read"),
            ["intelligence"] = ("Inteligência Organizacional", "Execuções de IA, evidências, modelos e revisão humana.", "/bff/admin/intelligence", "intelligence.read"),
            ["benchmark"] = ("Benchmark", "Referências, amostras mínimas e comparativos anonimizados.", "/bff/admin/benchmark", "benchmark.read"),
            ["one-on-one"] = ("One-on-One", "Conversas, compromissos e acompanhamento de lideranças.", "/bff/admin/one-on-one", "one_on_one.read"),
            ["evolution"] = ("Jornada de Evolução", "Marcos, indicadores e memória organizacional.", "/bff/admin/evolution", "evolution.read"),
            ["action-plans"] = ("Planos de Ação", "Ações, responsáveis, prazos e progresso.", "/bff/admin/action-plans", "action.read"),
            ["integrations"] = ("Integrações", "API keys, webhooks, Power BI, e-mail e conexões.", "/bff/integrations", "integrations.read"),
            ["notifications"] = ("Notificações", "Templates, entregas, leitura, erros e reenvios.", "/bff/notifications", "notifications.read"),
            ["audit"] = ("Auditoria", "Ações administrativas com filtros e rastreabilidade.", "/bff/audit", "audit.read"),
            ["jobs"] = ("Jobs e Processamentos", "Filas, tentativas, retries, falhas e correlações.", "/bff/admin/jobs", "jobs.read"),
            ["logs"] = ("Logs do Sistema", "Eventos sanitizados, referências e saúde operacional.", "/bff/platform-governance", "logs.read"),
            ["settings"] = ("Configurações", "Plataforma, sessão, LGPD, IA e relatórios.", "/bff/settings", "settings.read"),
            ["branding"] = ("Aparência e Marca", "Identidade visual e previews das experiências.", "/bff/organization/branding", "settings.read"),
            ["support"] = ("Suporte", "Tickets, prioridades, responsáveis e correlation IDs.", "/bff/admin/support", "support.read")
        };

    [HttpGet("Administration")]
    [HttpGet("Administration/Overview")]
    public IActionResult Index()
    {
        ViewData["Modules"] = Modules;
        return View();
    }

    [HttpGet("Administration/{module}")]
    public IActionResult Module(string module)
    {
        if (!Modules.TryGetValue(module, out var definition)) return NotFound();
        ViewData["Title"] = definition.Title;
        ViewData["Subtitle"] = definition.Subtitle;
        ViewData["Endpoint"] = definition.Endpoint;
        ViewData["Module"] = module;
        ViewData["Permission"] = definition.Permission;
        return View("Module");
    }

    public IActionResult Organizations() => Module("organizations");
    public IActionResult Users() => Module("users");
    public IActionResult Roles() => Module("roles");
    public IActionResult Plans() => Module("plans");
    public IActionResult FeatureFlags() => Module("feature-flags");
    public IActionResult Diagnostics() => Module("diagnostics");
    public IActionResult Questions() => Module("questions");
    public IActionResult Responses() => Module("responses");
    public IActionResult Results() => Module("results");
    public IActionResult Reports() => Module("reports");
    public IActionResult Certificates() => Module("certificates");
    public IActionResult Intelligence() => Module("intelligence");
    public IActionResult Benchmark() => Module("benchmark");
    public IActionResult OneOnOne() => Module("one-on-one");
    public IActionResult Evolution() => Module("evolution");
    public IActionResult ActionPlans() => Module("action-plans");
    public IActionResult Integrations() => Module("integrations");
    public IActionResult NotificationsAdmin() => Module("notifications");
    public IActionResult Audit() => Module("audit");
    public IActionResult Jobs() => Module("jobs");
    public IActionResult Logs() => Module("logs");
    public IActionResult Settings() => Module("settings");
    public IActionResult Branding() => Module("branding");
    public IActionResult Support() => Module("support");

    [HttpGet("Privacy")]
    public IActionResult Privacy() => RedirectToAction(nameof(Module), new { module = "settings" });

    [HttpGet("Notifications")]
    public IActionResult Notifications() => RedirectToAction(nameof(Module), new { module = "notifications" });

    [HttpGet("PlatformGovernance")]
    public IActionResult Governance() => RedirectToAction(nameof(Module), new { module = "audit" });

    [HttpGet("SystemHealth")]
    public IActionResult SystemHealth() => RedirectToAction(nameof(Module), new { module = "logs" });
}
