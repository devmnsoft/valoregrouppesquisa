using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class OrganizationalCentersController : Controller
{
    [HttpGet("Priorities")] public IActionResult Priorities() => Center("Prioridades", "Decida o que merece atenção agora", "Riscos, regressões e oportunidades são ordenados por impacto, urgência e evidência.", "As prioridades serão consolidadas após o processamento do diagnóstico.", "/Diagnostics");
    [HttpGet("Evidence")] public IActionResult Evidence() => Center("Evidências", "Entenda o que sustenta cada leitura", "Consulte origem, confiança, limitações e vínculos usados pela Inteligência Organizacional.", "As evidências serão consolidadas após o processamento do diagnóstico.", "/Diagnostics");
    [HttpGet("Evidence/Details/{id:guid}")] public IActionResult EvidenceDetails(Guid id) => Center("Detalhe da evidência", "Rastreabilidade da evidência", $"Referência: {id}. Confira contexto, classificação e usos autorizados.", "Esta evidência ainda não possui vínculos publicados.", "/Evidence");
    [HttpGet("Evidence/ByDiagnostic/{id:guid}")] public IActionResult EvidenceByDiagnostic(Guid id) => Center("Evidências do diagnóstico", "Leitura consolidada por ciclo", $"Diagnóstico: {id}. Evidências quantitativas e qualitativas permanecem separadas e rastreáveis.", "A coleta ainda não produziu evidências processadas.", "/Diagnostics");
    [HttpGet("Indexes")] public IActionResult Indexes() => Center("Índices Valora™", "Capacidades organizacionais em perspectiva", "IMO, ICS, IIO, IGO, ICO, ILI, IPO, IDO, IAC, IAR, IIS e ISO conectam pontuação, riscos e evolução.", "Feche e processe um diagnóstico para calcular os índices.", "/Diagnostics");
    [HttpGet("Indexes/Details/{code}")] public IActionResult IndexDetails(string code) => Center(code.ToUpperInvariant(), "Composição do índice", "Veja fatores, evidências, riscos, oportunidades, recomendações e histórico deste índice.", "Ainda não há ciclos suficientes para apresentar a evolução deste índice.", "/Indexes");
    [HttpGet("Radar")] public IActionResult Radar() => Center("Radar Valora™", "Equilíbrio do sistema organizacional", "Compare capacidades sem reduzir a organização a uma nota isolada.", "O radar será exibido após o cálculo dos índices.", "/Diagnostics");

    private IActionResult Center(string title, string eyebrow, string description, string empty, string cta)
    {
        ViewData["Title"] = title;
        return View("~/Views/OrganizationalCenters/Center.cshtml", new OrganizationalCenterViewModel(title, eyebrow, description, empty, cta));
    }
}

public sealed record OrganizationalCenterViewModel(string Title, string Eyebrow, string Description, string EmptyMessage, string CtaUrl);
