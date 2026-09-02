using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Advisor;
using Valora.Application.Common;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class AdvisorController(ICurrentOrganizationProvider current,AdvisorConversationService conversations,AdvisorMessageService messages,AdvisorContextBuilderService context,AdvisorPromptTemplateService templates):Controller
{
    [HttpGet] public async Task<IActionResult> Index(Guid? id,CancellationToken ct)
    {
        var scope=current.GetCurrent();if(!scope.IsResolved){TempData["AdvisorError"]="Selecione uma organização para usar o Valora Advisor™.";return View(new AdvisorIndexViewModel());}
        var list=await conversations.List(scope.RequireOrganizationId(),UserId(),ct);var active=id.HasValue?await conversations.Get(scope.RequireOrganizationId(),UserId(),id.Value,ct):null;
        return View(new AdvisorIndexViewModel{Conversations=list,ActiveConversation=active,ContextOptions=await context.Options(scope.RequireOrganizationId(),ct),Ask=new(){ConversationId=id}});
    }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Ask(AdvisorAskForm form,CancellationToken ct)
    {
        if(!ModelState.IsValid){TempData["AdvisorError"]="Revise a pergunta e selecione pelo menos uma evidência.";return RedirectToAction(nameof(Index),new{id=form.ConversationId});}
        var scope=current.GetCurrent();if(!scope.IsResolved){TempData["AdvisorError"]="Selecione uma organização antes de iniciar a análise.";return RedirectToAction(nameof(Index));}
        try
        {
            var id=form.ConversationId??await conversations.Create(scope.RequireOrganizationId(),UserId(),new(){Objective=form.Objective},ct);
            var selected=form.SourceKeys.Select(ParseSource).Where(x=>x is not null).Cast<AdvisorContextSelection>().ToArray();
            await messages.Send(scope.RequireOrganizationId(),UserId(),id,new(){Content=form.Question,Context=selected},ct);return RedirectToAction(nameof(Index),new{id});
        }
        catch(Exception e) when(e is ArgumentException or InvalidOperationException or KeyNotFoundException){TempData["AdvisorError"]=e.Message;return RedirectToAction(nameof(Index),new{id=form.ConversationId});}
    }
    [HttpGet] public async Task<IActionResult> Conversations(CancellationToken ct)=>View("List",await Page("Conversas","Histórico auditável de leituras organizacionais.",ct));
    [HttpGet] public async Task<IActionResult> Templates(CancellationToken ct)=>View("List",await Page("Templates","Prompts versionados, sujeitos a revisão humana e à Metodologia Valora™.",ct));
    [HttpGet,Authorize(Roles="admin,admin_valora,super_admin")] public IActionResult Guardrails()=>View("List",new AdvisorListPageViewModel("Guardrails","Bloqueios preservam evidência, limites organizacionais e decisão humana.",[],[]));
    [HttpGet] public IActionResult Feedback()=>View("List",new AdvisorListPageViewModel("Feedback","Avaliações ajudam a revisar respostas sem reescrever a evidência original.",[],[]));
    private async Task<AdvisorListPageViewModel> Page(string title,string description,CancellationToken ct){var o=current.GetCurrent();if(!o.IsResolved)return new(title,description,[],[]);return new(title,description,await conversations.List(o.RequireOrganizationId(),UserId(),ct),await templates.List(o.RequireOrganizationId(),ct));}
    private Guid UserId()=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:Guid.Empty;
    private static AdvisorContextSelection? ParseSource(string key){var parts=key.Split(':',2);return parts.Length==2&&Guid.TryParse(parts[1],out var id)?new(parts[0],id):null;}
}
