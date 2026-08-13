using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class IntelligenceController : Controller
{
    private static readonly Dictionary<string, IntelligenceWorkspaceViewModel> Workspaces = new(StringComparer.OrdinalIgnoreCase)
    {
        ["evidence"] = W("evidence", "Evidências Organizacionais", "Rastreabilidade", "Consulte a origem verificável de cada leitura.", "evidence", "evidência", ["tipo", "mappingStatus", "conceptCode", "metricCode", "indexCode"], ["questionId", "normalizedValue", "weight", "confidenceWeight", "mappingStatus"], "Respostas qualitativas são preservadas sem interpretação automática."),
        ["metrics"] = W("metrics", "Valora Metrics™", "Indicadores em contexto", "Avalie métricas junto da fórmula, evidências, tendência e limitações.", "metrics", "métrica", ["status", "conceptCode", "indexCode"], ["value", "formula", "evidenceCount", "trend", "limitation"], "Uma métrica isolada não constitui conclusão."),
        ["indices"] = W("indices", "Índices Valora™", "Maturidade em contexto", "Explore os doze índices oficiais e sua composição metodológica.", "indices", "índice", ["status", "level"], ["score", "level", "confidence", "evidenceCount", "components", "limitation"], "Sem composição e evidência suficientes, o índice permanece inconclusivo."),
        ["inference"] = W("inference", "Motor de Inferência Valora™", "Hipóteses rastreáveis", "Acompanhe o caminho entre evidência, regra aplicada e causa provável.", "inference", "inferência", ["trigger", "status", "confidence"], ["runId", "rule", "evidenceCount", "probableCause", "systems", "cascadeEffect", "limitation"], "Causa provável é hipótese; nunca certeza."),
        ["insights"] = W("insights", "Valora Insights IA™", "Inteligência interpretativa", "Priorize leituras objetivas, rastreáveis e sustentadas por evidências.", "insights", "insight", ["type", "priority", "confidence", "status"], ["evidence", "inference", "systems", "impact", "recommendation", "limitation"], "Nenhuma recomendação é produzida sem evidência suficiente."),
        ["action"] = W("action", "Valora Action™", "Evolução em execução", "Gerencie compromissos conectados a capacidade, indicador e evidência.", "action-plans", "ação", ["status", "priority", "owner", "capability"], ["evidenceJustification", "indicators", "completionCriteria", "executiveSponsor", "dueAt", "status"], "Action não é uma lista de tarefas: cada compromisso precisa de evidência e critério de conclusão."),
        ["evolution"] = W("evolution", "Valora Evolution™", "Leitura longitudinal", "Compare ciclos reais, estabilidade, regressão e velocidade observada.", "evolution", "ciclo", ["classification"], ["cycleAt", "maturityIndex", "change", "classification", "estimatedNextCycle"], "Primeiro ciclo registrado. A evolução longitudinal será calculada a partir do próximo ciclo."),
        ["journey"] = W("journey", "Valora Journey™", "Memória organizacional", "Revise marcos estratégicos automáticos e manuais, não logs técnicos.", "journey", "evento", ["eventType", "capability"], ["occurredAt", "eventType", "description", "metadata"], "A narrativa apresenta somente eventos organizacionais autorizados."),
        ["heatmap"] = W("heatmap", "Valora Heatmap™", "Mapa de atenção", "Investigue scores, riscos e evidências por capacidade.", "heatmap", "célula", ["classification", "risk"], ["score", "classification", "risk", "evidenceCount", "interpretation"], "Uma célula sinaliza onde investigar; não determina uma causa."),
        ["radar"] = W("radar", "Valora Radar™", "Equilíbrio sistêmico", "Compare dimensões fortes, frágeis e gaps com interpretação contextual.", "radar", "dimensão", ["cycle", "classification"], ["score", "previousScore", "gap", "evidenceCount", "interpretation"], "Sem ciclo anterior, nenhuma comparação é projetada."),
        ["benchmark"] = W("benchmark", "Valora Benchmark™", "Referência privada", "Compare histórico, áreas, unidades, capacidades, índices e ciclos com amostra mínima.", "benchmark", "comparativo", ["scope", "cycle", "indexCode"], ["sampleSize", "difference", "interpretation", "limitation"], "Ainda não existem dados suficientes para gerar um Benchmark Externo confiável para este perfil organizacional."),
        ["executive-report"] = W("executive-report", "Valora Executive Report™", "Devolutiva executiva", "Gere um preview rastreável por diagnóstico e ciclo.", "executive-report", "relatório", ["cycle", "status"], ["executiveSummary", "risks", "priorities", "limitations", "createdAt"], "Exportação PDF ainda não configurada neste ambiente.", primaryAction: "preview"),
        ["one-on-one"] = W("one-on-one", "Valora One-on-One™", "Conversas responsáveis", "Estruture pautas e compromissos sem expor respostas individuais.", "one-on-one", "reunião", ["status", "leader", "team"], ["leader", "team", "agenda", "commitments", "createdAt"], "Não classifica pessoas, não diagnostica psicologicamente e não recomenda promoção ou demissão."),
        ["platform-governance"] = W("platform-governance", "Governança da Plataforma", "Integridade do SaaS", "Audite permissões, alterações e entidades afetadas sem confundir governança organizacional.", "platform-governance", "evento", ["module", "user", "type"], ["before", "after", "correlationId", "runId", "justification", "entity"], "Governança da Plataforma trata rastreabilidade, segurança e controle do SaaS."),
        ["integrations"] = W("integrations", "Integrações / Power BI™", "Dados sob autorização", "Consulte conectores, exportações agregadas e auditoria de acesso.", "integrations", "conector", ["status", "type"], ["status", "lastExportAt", "format", "authorization", "audit"], "Integração Power BI™ preparada para configuração. Nenhum dado será enviado sem autorização.")
    };
    [HttpGet("Intelligence")]
    [HttpGet("InteligenciaOrganizacional")]
    public IActionResult Index() => View();

    public IActionResult Evidence() => Workspace("evidence");
    public IActionResult Metrics() => Workspace("metrics");
    public IActionResult Indices() => Workspace("indices");
    public IActionResult Inference() => Workspace("inference");
    public IActionResult Insights() => Workspace("insights");
    public IActionResult Action() => Workspace("action");
    public IActionResult Evolution() => Workspace("evolution");
    public IActionResult Journey() => Workspace("journey");
    public IActionResult Heatmap() => Workspace("heatmap");
    public IActionResult Radar() => Workspace("radar");
    public IActionResult Benchmark() => Workspace("benchmark");
    public IActionResult ExecutiveReport() => Workspace("executive-report");
    public IActionResult OneOnOne() => Workspace("one-on-one");
    public IActionResult PlatformGovernance() => Workspace("platform-governance");
    public IActionResult Integrations() => Workspace("integrations");

    [HttpGet("Intelligence/{module}")]
    public IActionResult Module(string module)
    {
        if (Workspaces.TryGetValue(module, out var workspace)) return View(ToViewName(module), workspace);
        var definition = IntelligenceModuleViewModel.Find(module);
        return definition is null ? NotFound() : View("Module", definition);
    }

    private static IntelligenceWorkspaceViewModel W(string slug, string title, string eyebrow, string purpose, string endpoint, string itemLabel, string[] filters, string[] fields, string limitation, string? notice = null, string? primaryAction = null) => new(slug, title, eyebrow, purpose, endpoint, itemLabel, filters, fields, limitation, notice, primaryAction);
    private static string ToViewName(string slug) => string.Concat(slug.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    private IActionResult Workspace(string slug) => View(ToViewName(slug), Workspaces[slug]);
}
