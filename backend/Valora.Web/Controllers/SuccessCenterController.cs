using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Models.ViewModels.SuccessCenter;
namespace Valora.Web.Controllers;
[Authorize,Route("SuccessCenter")]
public sealed class SuccessCenterController:Controller
{
 [HttpGet("")] public IActionResult Index()=>Page("overview","Visão Geral","Acompanhe ativação, saúde, suporte e adoção sem perder a rastreabilidade.");
 [HttpGet("Onboarding")] public IActionResult Onboarding()=>Page("onboarding","Onboarding","Checklist de implantação com responsável, prazo, evidência e orientação executiva.");
 [HttpGet("Health")] public IActionResult Health()=>Page("health","Saúde da Conta","Score explicável: nenhum risco é apresentado sem evidência registrada.");
 [HttpGet("Support")] public IActionResult Support()=>Page("support","Chamados","Atendimento isolado por organização e histórico integral de cada interação.");
 [HttpGet("Support/Create")] public IActionResult Create()=>View("CreateSupport",new CreateSupportTicketViewModel());
 [ValidateAntiForgeryToken,HttpPost("Support/Create")] public IActionResult Create(CreateSupportTicketViewModel model){if(!ModelState.IsValid)return View("CreateSupport",model);TempData["Success"]="Chamado validado. O envio será registrado com rastreabilidade.";return RedirectToAction(nameof(Support));}
 [HttpGet("Support/Details/{id:guid}")] public IActionResult Details(Guid id)=>View("SupportDetails",new SupportTicketDetailsViewModel(id,"Chamado", "dúvida operacional","normal","open",[]));
 [HttpGet("KnowledgeBase")] public IActionResult KnowledgeBase()=>Page("knowledge","Base de Conhecimento","Encontre orientação por contexto, plano e etapa da jornada.");
 [HttpGet("KnowledgeBase/Article/{id:guid}")] public IActionResult Article(Guid id)=>View("Article",new KnowledgeArticleViewModel(id,"Artigo da base de conhecimento","Conteúdo autorizado para sua organização.","O artigo será carregado no escopo do plano e da organização autenticada.","Orientação"));
 [HttpGet("Playbooks")] public IActionResult Playbooks()=>Page("playbooks","Playbooks","Implantação e recuperação guiadas por tarefas, resultados e evidências.");
 [HttpGet("Usage")] public IActionResult Usage()=>Page("usage","Uso do Produto","Adoção baseada exclusivamente em eventos reais do produto.");
 private IActionResult Page(string section,string title,string description)=>View("Index",new SuccessCenterPageViewModel("VALORA SUCCESS CENTER™",title,description,section,[new("Em acompanhamento","—","Dados do escopo autorizado"),new("Exigem atenção","—","Somente com evidência"),new("Próximo passo","Consultar","Jornada orientada")]));
}
