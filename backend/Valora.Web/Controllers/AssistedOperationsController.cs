using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class AssistedOperationsController : Controller
{
    [Route("Support"), Route("Support/Tickets"), Route("Support/Tickets/{id:guid}"), Route("Platform/Support")] public IActionResult Support()=>Page("support","Central de Suporte","Acompanhe chamados, prioridades, responsáveis e resoluções.");
    [Route("Feedback"), Route("Platform/Feedback"), Route("CustomerSuccess/Feedback")] public IActionResult Feedback()=>Page("feedback","Feedback do Cliente","Transforme a experiência real dos clientes em evolução priorizada.");
    [Route("CustomerSuccess"), Route("CustomerSuccess/Organizations/{id:guid}"), Route("Platform/CustomerSuccess")] public IActionResult CustomerSuccess()=>Page("customer-success","Customer Success","Saúde explicável, riscos e próximos passos de cada organização.");
    [Route("UsageAnalytics"), Route("Platform/Usage"), Route("CustomerSuccess/Usage")] public IActionResult Usage()=>Page("usage-analytics","Métricas de Adoção","Uso agregado e sinais de ativação do produto.");
    [Route("Onboarding"), Route("CustomerSuccess/Onboarding")] public IActionResult Onboarding()=>Page("onboarding","Onboarding Operacional","Checklist pós-venda baseado em evidências reais.");
    [Route("Commercial/UpgradeRequests"), Route("Plans/UpgradeRequests"), Route("CustomerSuccess/UpgradeRequests")] public IActionResult Upgrades()=>Page("upgrade-requests","Solicitações Comerciais","Oportunidades de expansão sem alteração automática do plano.");
    [Route("Platform/Incidents"), Route("SystemHealth/Incidents")] public IActionResult Incidents()=>Page("incidents","Incidentes Operacionais","Investigação, mitigação e aprendizado com rastreabilidade.");
    [AllowAnonymous,Route("ReleaseNotes"), Route("Platform/ReleaseNotes")] public IActionResult Releases()=>Page("release-notes","Release Notes","Evolução controlada e transparente do Valora Insight™.");
    [Route("Platform/DataQuality"), Route("SystemHealth/DataQuality")] public IActionResult DataQuality()=>Page("data-quality","Qualidade dos Dados","Verificações não destrutivas e histórico operacional.");
    private IActionResult Page(string module,string title,string subtitle){ViewData["Title"]=title;ViewData["Module"]=module;ViewData["Subtitle"]=subtitle;return View("Index");}
}
