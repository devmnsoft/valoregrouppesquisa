using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize(Roles = "admin_valora,empresa_admin")]
[Route("SystemHealth")]
public sealed class SystemHealthController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(Page("Reliability Center™", "Banco, API, BFF, workers, filas e processamentos em uma visão operacional.", "Componentes saudáveis", "Alertas", "Erros críticos"));

    [HttpGet("Errors")]
    public IActionResult Errors() => View("Index", Page("Erros do Sistema", "Mensagens sanitizadas e correlation IDs para investigação autorizada.", "Críticos", "Avisos", "Resolvidos"));

    [HttpGet("Jobs")]
    public IActionResult Jobs() => View("Index", Page("Jobs e Processamentos", "Execuções, duração, falhas e reprocessamento governado.", "Em execução", "Concluídos", "Falhas"));

    private static SecurityCompliancePageViewModel Page(string title, string description, params string[] metrics) =>
        new(title, description, metrics.Select(value => new SecurityMetricViewModel(value, 0)).ToArray(),
            ["Atualização baseada somente em checks persistidos.", "Detalhes técnicos são restritos a perfis autorizados.", "Use o correlation ID ao acionar o suporte."]);
}
