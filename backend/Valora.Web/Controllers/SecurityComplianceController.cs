using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Models.ViewModels;
namespace Valora.Web.Controllers;

[Authorize(Roles="admin_valora,empresa_admin")]
[Route("SecurityCompliance")]
public sealed class SecurityComplianceController : Controller
{
 [HttpGet("")] public IActionResult Index()=>Page("Security & Compliance Center™","Governança LGPD, riscos e evidências em uma visão executiva.","Conformidade LGPD","Incidentes abertos","Revisões pendentes","Acessos sensíveis");
 [HttpGet("Privacy")] public IActionResult Privacy()=>Page("Privacidade e LGPD","Consentimentos versionados, revogáveis e rastreáveis.","Consentimentos ativos","Revogações recentes","Configurações");
 [HttpGet("DataRequests")] public IActionResult DataRequests()=>Page("Solicitações de Titulares","Acesso, correção, anonimização, exclusão responsável e portabilidade.","Em aberto","Próximas do prazo","Concluídas");
 [HttpGet("Retention")] public IActionResult Retention()=>Page("Retenção de Dados","Políticas por categoria com ação segura após expiração.","Políticas ativas","Execuções pendentes","Itens anonimizados");
 [HttpGet("Audit")] public IActionResult Audit()=>Page("Auditoria Avançada","Trilha pesquisável, isolada por organização e sustentada por evidências.","Eventos hoje","Falhas de acesso","Exportações");
 [HttpGet("Incidents")] public IActionResult Incidents()=>Page("Incidentes de Segurança","Investigação, impacto, resposta e resolução com histórico imutável.","Críticos","Em investigação","Resolvidos");
 [HttpGet("AccessReviews")] public IActionResult AccessReviews()=>Page("Revisão de Acessos","Revalidação humana de usuários, perfis, chaves e integrações.","Ciclos abertos","Itens pendentes","Riscos identificados");
 [HttpGet("SensitiveAccess")] public IActionResult SensitiveAccess()=>Page("Acessos Sensíveis","Quem acessou, finalidade, recurso, decisão e correlação.","Permitidos","Negados","Sem finalidade");
 [HttpGet("Access")] public IActionResult Access()=>View("SensitiveAccess",Model("Controle de Acesso","Permissões efetivas, recusas e tentativas suspeitas da organização selecionada.","Acessos negados","Tentativas suspeitas","Revisões pendentes"));
 [HttpGet("ApiKeys")] public IActionResult ApiKeys()=>View("Index",Model("Chaves de API","Crie credenciais com escopo mínimo, acompanhe o último uso e revogue imediatamente quando necessário.","Chaves ativas","Uso recente","Revogadas"));
 [ValidateAntiForgeryToken,HttpPost("Privacy/Register")] public IActionResult RegisterConsent(ConsentInputViewModel model)=>Command(model,nameof(Privacy),"privacy.consent.register");
 [ValidateAntiForgeryToken,HttpPost("DataRequests/Create")] public IActionResult CreateRequest(DataRequestInputViewModel model)=>Command(model,nameof(DataRequests),"privacy.request.create");
 [ValidateAntiForgeryToken,HttpPost("Retention/Configure")] public IActionResult ConfigureRetention(RetentionInputViewModel model)=>Command(model,nameof(Retention),"retention.configure");
 [ValidateAntiForgeryToken,HttpPost("Incidents/Register")] public IActionResult RegisterIncident(IncidentInputViewModel model)=>Command(model,nameof(Incidents),"security.incident.register");
 private IActionResult Command(object model,string action,string command){if(!ModelState.IsValid){TempData["SecurityComplianceError"]="Revise os campos obrigatórios.";return RedirectToAction(action);}TempData["SecurityComplianceCommand"]=command;return RedirectToAction(action);}
 private ViewResult Page(string title,string description,params string[] metrics)=>View(Model(title,description,metrics));
 private static SecurityCompliancePageViewModel Model(string title,string description,params string[] metrics)=>new(title,description,metrics.Select(x=>new SecurityMetricViewModel(x,0)).ToArray(),["Nenhum dado é presumido: métricas sem evidência permanecem zeradas.","Toda operação sensível exige organização autenticada e gera rastreabilidade.","Decisões de descarte e incidentes permanecem sob responsabilidade humana."]);
}
