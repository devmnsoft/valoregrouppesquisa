using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.SuccessCenter;
using Valora.Application.Access;
using Valora.Web.Models.ViewModels.SuccessCenter;
namespace Valora.Web.Controllers;

[Authorize, Route("SuccessCenter")]
public sealed class SuccessCenterController(ICurrentOrganizationProvider current, SupportTicketService tickets, ILogger<SuccessCenterController> logger) : Controller {
    [HttpGet("")] public IActionResult Index() => Page("overview", "Visão Geral", "Acompanhe ativação, saúde, suporte e adoção sem perder a rastreabilidade.");
    [HttpGet("Onboarding")] public IActionResult Onboarding() => Page("onboarding", "Onboarding", "Checklist de implantação com responsável, prazo, evidência e orientação executiva.");
    [HttpGet("Health")] public IActionResult Health() => Page("health", "Saúde da Conta", "Indicadores explicáveis: nenhum risco é apresentado sem evidência registrada.");
    [HttpGet("Support")] public async Task<IActionResult> Support([FromQuery] SupportTicketQuery query,CancellationToken ct) { if (!TryContext(out var o, out var u)) return View("MissingOrganization"); try{return View("Support", new SupportTicketListViewModel(await tickets.List(o,u,CanManage,query,ct),query,TempData["Success"]?.ToString(), TempData["Error"]?.ToString()));}catch(ValidationException e){logger.LogWarning(e,"Invalid support filters");return View("Support",new SupportTicketListViewModel(new([],1,query.ValidPageSize,0),query,null,"Revise os filtros informados."));} }
    [HttpGet("Support/Create")] public IActionResult Create() => TryContext(out _, out _) ? View("CreateSupport", new CreateSupportTicketViewModel()) : View("MissingOrganization");
    [ValidateAntiForgeryToken, HttpPost("Support/Create")]
    public async Task<IActionResult> Create(CreateSupportTicketViewModel model, CancellationToken ct) {
        if (!TryContext(out var o, out var u)) return Forbid();
        if (!ModelState.IsValid) return View("CreateSupport", model);
        try { var id = await tickets.Create(o, u, new CreateTicketCommand { Subject=model.Subject, Description=model.Description, Category=model.Category, Priority=model.Priority }, ct); TempData["Success"] = "Chamado aberto e registrado com sucesso."; return RedirectToAction(nameof(Details), new { id }); }
        catch (ValidationException e) { ModelState.AddModelError(string.Empty, e.Message); return View("CreateSupport", model); }
    }
    [HttpGet("Support/Details/{id:guid}")] public async Task<IActionResult> Details(Guid id, CancellationToken ct) { if (!TryContext(out var o, out var u)) return Forbid(); var detail = await tickets.Get(o,u,id,CanManage,CanManage,ct); return detail is null ? NotFound() : View("SupportDetails", SupportTicketDetailsViewModel.From(detail, TempData["Success"]?.ToString(), TempData["Error"]?.ToString())); }
    [ValidateAntiForgeryToken, HttpPost("Support/Details/{id:guid}/Reply")]
    public async Task<IActionResult> Reply(Guid id, TicketReplyViewModel model, CancellationToken ct) { if (!TryContext(out var o, out var u)) return Forbid(); var allowed=await tickets.Get(o,u,id,CanManage,false,ct);if(allowed is null)return NotFound(); if (!ModelState.IsValid) return View("SupportDetails", SupportTicketDetailsViewModel.From(allowed, null, "Escreva uma resposta entre 2 e 4.000 caracteres.", model)); try { await tickets.Reply(o,u,id,model.Message,ct); TempData["Success"]="Resposta registrada."; } catch (Exception e) when(e is InvalidOperationException or ValidationException) {logger.LogWarning(e,"Support reply rejected. TicketId={TicketId}",id);return View("SupportDetails",SupportTicketDetailsViewModel.From(allowed,null,"Não foi possível registrar a resposta. Revise o conteúdo e tente novamente.",model));} return RedirectToAction(nameof(Details),new{id}); }
    [ValidateAntiForgeryToken, HttpPost("Support/Details/{id:guid}/Resolve")] public async Task<IActionResult> Resolve(Guid id, CancellationToken ct) => await Transition(id, (o,u)=>tickets.Resolve(o,u,id,ct));
    [ValidateAntiForgeryToken, HttpPost("Support/Details/{id:guid}/Reopen")] public async Task<IActionResult> Reopen(Guid id, CancellationToken ct) => await Transition(id, (o,u)=>tickets.Reopen(o,u,id,ct));
    [HttpGet("KnowledgeBase")] public IActionResult KnowledgeBase() => Page("knowledge", "Base de Conhecimento", "Encontre orientação por contexto, plano e etapa da jornada.");
    [HttpGet("KnowledgeBase/Article/{id:guid}")] public IActionResult Article(Guid id) => View("Article", new KnowledgeArticleViewModel(id, "Artigo da base de conhecimento", "Conteúdo autorizado para sua organização.", "O artigo será carregado no escopo do plano e da organização autenticada.", "Orientação"));
    [HttpGet("Playbooks")] public IActionResult Playbooks() => Page("playbooks", "Playbooks", "Implantação e recuperação guiadas por tarefas, resultados e evidências.");
    [HttpGet("Usage")] public IActionResult Usage() => Page("usage", "Uso do Produto", "Adoção baseada exclusivamente em eventos reais do produto.");
    private async Task<IActionResult> Transition(Guid id, Func<Guid,Guid,Task> command) { if (!TryContext(out var o,out var u)) return Forbid(); if(await tickets.Get(o,u,id,CanManage,false,CancellationToken.None) is null)return NotFound(); try { await command(o,u); TempData["Success"]="Situação atualizada e registrada no histórico."; } catch (Exception e) when (e is InvalidOperationException or KeyNotFoundException) { logger.LogWarning(e,"Support transition rejected. TicketId={TicketId} OrganizationId={OrganizationId}",id,o); TempData["Error"]="A situação não pôde ser alterada. Atualize a página e tente novamente."; } return RedirectToAction(nameof(Details),new{id}); }
    private bool CanManage=>User.IsInRole("admin_valora")||User.IsInRole("admin_cliente")||User.Claims.Any(x=>(x.Type is "permission" or "permissions")&&x.Value.Equals(ValoraPermissions.SupportTickets.Manage,StringComparison.OrdinalIgnoreCase));
    private bool TryContext(out Guid organizationId, out Guid userId) { var scope=current.GetCurrent(); organizationId=scope.IsResolved?scope.RequireOrganizationId():Guid.Empty; userId=Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)??User.FindFirstValue("sub"),out var id)?id:Guid.Empty; return organizationId!=Guid.Empty&&userId!=Guid.Empty; }
    private IActionResult Page(string section, string title, string description) => View("Index", new SuccessCenterPageViewModel("VALORA SUCCESS CENTER™", title, description, section, [new("Em acompanhamento", "—", "Dados do escopo autorizado"), new("Exigem atenção", "—", "Somente com evidência"), new("Próximo passo", "Consultar", "Jornada orientada")]));
}
